using System.Collections.Concurrent;
using RTWServer.ServerCore.Interface;

namespace RTWServer.ServerCore.implementation;

/// <summary>
/// userId → 현재 세션 인덱스. 단일 세션 강제(userId당 최대 하나)와 last-wins 교체의
/// 동시성 민감한 로직(CAS 루프, pair 단위 제거)을 한곳에 격리해 테스트와 추론을 쉽게 한다.
/// 실제 세션 종료(킥)는 호출자가 담당한다 — 레지스트리는 누구를 밀어냈는지만 알려준다.
/// </summary>
public sealed class UserSessionRegistry
{
    private readonly ConcurrentDictionary<long, IClientSession> _byUser = new();

    /// <summary>
    /// 세션을 등록한다. 같은 userId의 기존 세션이 밀려나면 그 세션을 반환한다(호출자가 종료시킬 대상).
    /// 밀려난 세션이 없거나 동일 세션 재등록이면 null.
    /// TryGetValue→TryUpdate/TryAdd CAS 루프로, 두 연결이 동시에 들어와도 정확히 하나만 남게 한다.
    /// </summary>
    public IClientSession? Register(IClientSession session)
    {
        long userId = session.UserId;

        while (true)
        {
            if (_byUser.TryGetValue(userId, out IClientSession? existing))
            {
                if (ReferenceEquals(existing, session))
                {
                    return null; // 이미 등록됨
                }

                if (_byUser.TryUpdate(userId, session, existing))
                {
                    return existing; // 교체 성공 → 밀려난 기존 세션을 호출자가 킥
                }
                // 경합으로 교체 실패 → 재시도
            }
            else if (_byUser.TryAdd(userId, session))
            {
                return null;
            }
            // 경합으로 추가 실패 → 재시도
        }
    }

    /// <summary>
    /// 세션을 인덱스에서 제거하되, 매핑된 값이 *이 세션*일 때만 제거한다(키·값 모두 일치).
    /// last-wins로 들어온 교체 세션을, 밀려난 옛 세션의 정리 과정이 실수로 지우는 것을 막는다.
    /// </summary>
    public void Unregister(IClientSession session)
    {
        _byUser.TryRemove(new KeyValuePair<long, IClientSession>(session.UserId, session));
    }

    public IClientSession? Get(long userId)
    {
        return _byUser.GetValueOrDefault(userId);
    }
}
