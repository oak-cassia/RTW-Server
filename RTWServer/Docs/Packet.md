### 패킷 종류
패킷 ID: 각 패킷을 구분하기 위한 고유 ID (예: ushort 또는 enum 타입)를 정의합니다. 이는 클라이언트와 서버 간의 약속입니다.
패킷 길이: 가변 길이 패킷을 사용할 경우, 패킷 헤더에 길이를 명시하여 수신 측에서 정확한 크기만큼 읽도록 합니다.

[시스템 및 연결]
C_AuthToken (클라이언트 -> 서버)
목적: 웹 서버 발급 토큰으로 실시간 서버 인증 요청
데이터: string AuthToken
S_AuthResult (서버 -> 클라이언트)
목적: 실시간 서버 인증 결과 통보
데이터: RTWErrorCode errorCode, int PlayerId (인증 성공 시 할당/확인된 플레이어 고유 ID)
C_Disconnect (클라이언트 -> 서버)
목적: 명시적 연결 해제 요청
데이터: (없음 또는 int ReasonCode)
S_Disconnected (서버 -> 클라이언트)
목적: 서버 측 연결 해제 통보
데이터: int ReasonCode (해제 이유)
S_SystemMessage (서버 -> 클라이언트)
목적: 서버 공지, 오류 등 일반 메시지 전달
데이터: string Message, byte MessageType (정보, 경고, 오류 등)

[월드 및 플레이어 관리]
C_EnterWorld (클라이언트 -> 서버)
목적: 인증 성공 후 게임 월드 진입 요청
데이터: int WorldId (선택 사항)
S_EnterWorldResult (서버 -> 클라이언트)
목적: 월드 진입 결과 및 초기 월드 상태(내 플레이어 정보) 전달
데이터: RTWErrorCode errorCode, 성공 시: Vector3 InitialPosition, float InitialHp, float InitialMp, byte InitialReputationLevel, long CurrentExp, int Level, long Gold (등 초기 정보)
S_SpawnPlayer (서버 -> 클라이언트)
목적: 내 주변에 다른 플레이어가 나타났음을 알림
데이터: int PlayerId, string Name, Vector3 Position, float Hp, byte State, byte ReputationLevel, List<EquippedItemInfo> EquippedItems (현재 장비 정보 요약)
S_DespawnPlayer (서버 -> 클라이언트)
목적: 내 주변에서 다른 플레이어가 사라졌음을 알림
데이터: int PlayerId
S_SpawnMonster (서버 -> 클라이언트)
목적: 내 주변에 몬스터가 나타났음을 알림
데이터: int MonsterInstanceId, int MonsterTemplateId, Vector3 Position, float Hp, byte State
S_DespawnMonster (서버 -> 클라이언트)
목적: 내 주변에서 몬스터가 사라졌음을 알림
데이터: int MonsterInstanceId
S_SpawnNPC (서버 -> 클라이언트)
목적: 내 주변에 NPC가 나타났음을 알림
데이터: int NpcId, int NpcTemplateId, Vector3 Position, string Name, byte State
S_DespawnNPC (서버 -> 클라이언트)
목적: 내 주변에서 NPC가 사라졌음을 알림
데이터: int NpcId
S_SpawnItem (서버 -> 클라이언트)
목적: 월드에 특정 아이템이 떨어져 있음을 알림 (줍기 가능 상태)
데이터: int ItemInstanceId, int ItemTemplateId, Vector3 Position, int Amount
S_SpawnGuard (서버 -> 클라이언트)
목적: 월드에 경비대 NPC가 나타났음을 알림
데이터: int GuardId, int GuardTemplateId, Vector3 Position, int TargetPlayerId (출동 대상 플레이어 ID, 없으면 0 또는 무효값), byte State (순찰, 추적 등)
S_DespawnGuard (서버 -> 클라이언트)
목적: 월드에서 경비대 NPC가 사라졌음을 알림
데이터: int GuardId
S_PlayerWantedStatus (서버 -> 클라이언트) - S_SpawnPlayer에 포함되거나 별도 패킷
목적: 특정 플레이어가 현재 수배 중인지 여부와 수배 레벨을 알림 (주변 플레이어에게도 보여줄 정보)
데이터: int PlayerId, byte WantedLevel (0: 수배 아님, 1이상: 수배 레벨), float TimeRemaining (수배 해제까지 남은 시간, 선택 사항)

[코어 실시간 기능]
C_Move (클라이언트 -> 서버)
목적: 플레이어 이동 정보 업데이트
데이터: Vector3 Position, Vector3 Velocity, byte MoveState, float Timestamp
S_Move (서버 -> 클라이언트)
목적: 특정 엔티티 이동 정보 전달
데이터: int EntityId, Vector3 Position, Vector3 Velocity, byte MoveState
S_EntityStateChange (서버 -> 클라이언트)
목적: 특정 엔티티의 일반 상태 변화 전달 (HP, MP, 상태 이상 등)
데이터: int EntityId, byte StateType, float/int/bool Value, int CasterId (선택 사항)
S_HpChange (서버 -> 클라이언트)
목적: 특정 대상의 HP 변화 정보 전달 (전투 결과 등)
데이터: int EntityId, float CurrentHp, float MaxHp, float ChangeAmount (선택 사항), int AttackerId (선택 사항)
S_PlayEffect (서버 -> 클라이언트)
목적: 시각/청각 효과 재생 요청
데이터: int EffectId, Vector3 Position, int TargetId (선택 사항), float Duration (선택 사항)
S_ReputationChange (서버 -> 클라이언트) - GTA5 컨셉
목적: 특정 플레이어의 평판/악명 변화 알림
데이터: int PlayerId, byte NewReputationLevel, int ChangeAmount (선택 사항)

[상호작용 및 액션]
C_Attack (클라이언트 -> 서버)
목적: 공격 요청
데이터: int TargetId, byte AttackType, Vector3 AttackDirection/Position
S_AttackResult (서버 -> 클라이언트)
목적: 공격 결과 통보 (피해량, 상태 변화 등)
데이터: int AttackerId, int TargetId, float DamageDealt, bool IsCritical, bool IsMiss, bool IsTargetDead, float TargetNewHp, byte EffectToPlayId (선택 사항)
C_InteractObject (클라이언트 -> 서버)
목적: 월드 오브젝트와 상호작용 요청
데이터: int ObjectId, byte InteractionType
S_ObjectStateChange (서버 -> 클라이언트)
목적: 월드 오브젝트 상태 변화 통보
데이터: int ObjectId, byte StateType, bool/int Value
C_TalkToNPC (클라이언트 -> 서버)
목적: 특정 NPC에게 대화 요청 (상점 열기, 퀘스트 받기 등 선행)
데이터: int NpcId
S_NPCDialogue (서버 -> 클라이언트)
목적: NPC 대화 내용 및 선택지 전달
데이터: int NpcId, int DialogueId, string DialogueText, List<DialogueOption>
C_DialogueResponse (클라이언트 -> 서버)
목적: 플레이어의 대화 선택지 응답
데이터: int NpcId, int DialogueId, int ChosenOptionId
C_QuestAction (클라이언트 -> 서버)
목적: 퀘스트 관련 행동 요청 (시작/완료/포기)
데이터: byte ActionType, int QuestId, int TargetNpcId (선택 사항)
S_QuestUpdate (서버 -> 클라이언트)
목적: 퀘스트 진행 상황 업데이트 통보
데이터: int QuestId, byte QuestState, string ProgressText, List<QuestObjectiveProgress>

[상점 및 경제]
C_RequestShopItems (클라이언트 -> 서버)
목적: 특정 상점 NPC/오브젝트가 판매하는 아이템 목록 요청
데이터: int ShopId
S_ShopItemList (서버 -> 클라이언트)
목적: 상점 판매 아이템 목록 전달
데이터: int ShopId, List<ShopItemInfo> (아이템 ID, 가격, 수량 등)
C_BuyItemFromShop (클라이언트 -> 서버)
목적: 상점에서 아이템 구매 요청
데이터: int ShopId, int ItemTemplateId, int Amount
S_BuyItemResult (서버 -> 클라이언트)
목적: 아이템 구매 결과 통보
데이터: RTWErrorCode errorCode, 성공 시: int ItemTemplateId, int Amount (어떤 아이템을 구매했는지 확인)
S_CurrencyChange (서버 -> 클라이언트)
목적: 플레이어 소지 화폐(골드 등) 변화 알림
데이터: byte CurrencyType (골드=1, 보석=2 등), long NewAmount

[인벤토리 및 장비]
S_InitialInventory (서버 -> 클라이언트) - 월드 진입 시 S_EnterWorldResult 내에 포함되거나 별도 패킷으로 전송
목적: 플레이어의 현재 인벤토리 목록 전달
데이터: List<ItemInfo> (인벤토리 내 모든 아이템 정보: 슬롯 번호, 아이템 ID, 수량, 고유 옵션 등)
S_AddItemToInventory (서버 -> 클라이언트)
목적: 인벤토리에 아이템이 추가/수량 변화되었음을 알림 (줍기, 구매, 보상 등)
데이터: int SlotIndex, int ItemTemplateId, int NewAmount, List<ItemStat> ItemStats (고유 옵션 등, 새 아이템일 경우)
S_RemoveItemFromInventory (서버 -> 클라이언트)
목적: 인벤토리에서 아이템이 제거/수량 변화되었음을 알림 (사용, 버리기, 판매, 강화 재료 소모 등)
데이터: int SlotIndex, int ItemTemplateId, int NewAmount (0이면 슬롯 비워짐)
C_UseItem (클라이언트 -> 서버)
목적: 인벤토리 아이템 사용 요청
데이터: int SlotIndex, int TargetId (사용 대상, 선택 사항)
C_DropItem (클라이언트 -> 서버)
목적: 인벤토리 아이템 월드에 버리기 요청
데이터: int SlotIndex, int Amount, Vector3 TargetPosition
C_EquipItem (클라이언트 -> 서버)
목적: 인벤토리 아이템 장비 장착 요청
데이터: int InventorySlotIndex
S_EquipResult (서버 -> 클라이언트)
목적: 장비 장착 결과 통보
데이터: RTWErrorCode errorCode, 성공 시: int InventorySlotIndex (사용한 인벤토리 슬롯), byte EquippedSlotType (장착된 장비 슬롯), int ItemTemplateId (장착된 아이템 ID)
C_UnequipItem (클라이언트 -> 서버)
목적: 장비 해제 요청
데이터: byte EquippedSlotType
S_UnequipResult (서버 -> 클라이언트)
목적: 장비 해제 결과 통보
데이터: RTWErrorCode errorCode, 성공 시: byte UnequippedSlotType, int InventorySlotIndex (들어간 인벤토리 슬롯), int ItemTemplateId (해제된 아이템 ID)
S_PlayerAppearanceChange (서버 -> 클라이언트) - 다른 플레이어에게 자신의 장비 변화 알림
목적: 특정 플레이어의 장비 외형 변화 통보
데이터: int PlayerId, List<EquippedItemInfo> (변경되거나 추가된 장비 슬롯/아이템 ID 목록)
C_EnhanceItem (클라이언트 -> 서버)
목적: 아이템 강화 요청
데이터: int TargetSlotIndex (강화 대상 아이템 슬롯), List<int> MaterialSlotIndices (재료 아이템 슬롯 목록), byte CurrencyType (사용할 화폐 타입), long CurrencyAmount (사용할 화폐량)
S_EnhanceItemResult (서버 -> 클라이언트)
목적: 아이템 강화 결과 통보
데이터: RTWErrorCode errorCode, 성공/실패 시: int TargetSlotIndex, int NewEnhancementLevel, bool IsDestroyed (실패 시 파괴 여부), List<ItemStat> NewStats (강화 성공 시 변경된 능력치 목록)

[성장 (레벨/경험치)]
S_GainExp (서버 -> 클라이언트)
목적: 경험치 획득 통보
데이터: long AmountGained (획득량), long CurrentTotalExp (누적 경험치), int SourceEntityId (경험치 획득 원인 엔티티 ID, 선택 사항 - 몬스터 ID 등)
S_LevelUp (서버 -> 클라이언트)
목적: 플레이어 레벨 업 통보
데이터: int NewLevel, long CurrentExp (레벨 업 후 경험치), long ExpToNextLevel (다음 레벨까지 필요 경험치), List<StatChange> ChangedStats (레벨 업으로 상승한 능력치 목록)

[소셜]
C_Chat (클라이언트 -> 서버)
목적: 채팅 메시지 전송
데이터: byte ChatType, string Message, string TargetName (선택 사항)
S_Chat (서버 -> 클라이언트)
목적: 채팅 메시지 수신
데이터: byte ChatType, int SenderPlayerId, string SenderName, string Message

[GTA5 판타지 특화 예시]
C_StealTarget (클라이언트 -> 서버)
목적: 훔치기 시도
데이터: int TargetId
S_StealResult (서버 -> 클라이언트)
목적: 훔치기 결과 통보
데이터: bool IsSuccess, bool IsCaught, List<ItemInfo> GainedItems, int DetectedByEntityId
C_UseSkill (클라이언트 -> 서버)
목적: 스킬 사용 요청
데이터: int SkillId, int TargetId (선택 사항), Vector3 TargetPosition (선택 사항)
S_UseSkill (서버 -> 클라이언트)
목적: 특정 플레이어가 스킬 사용했음 알림
데이터: int PlayerId, int SkillId, int TargetId, Vector3 TargetPosition
C_MountVehicle (클라이언트 -> 서버)
목적: 탈것 탑승/하차 요청
데이터: int MountEntityId, bool IsMounting
S_MountVehicle (서버 -> 클라이언트)
목적: 특정 플레이어 탈것 탑승/하차 알림
데이터: int PlayerId, int MountEntityId, bool IsMounted
S_WantedLevelChange (서버 -> 클라이언트) - 자신에게만 통보
목적: 플레이어 자신의 수배 레벨 변화 통보
데이터: byte NewWantedLevel, string Reason (수배 레벨이 오르거나 내려간 이유, 예: "상인 공격", "경비병 사살", "시간 경과"), float TimeRemaining (수배 해제까지 남은 시간, 선택 사항)
C_SurrenderToGuard (클라이언트 -> 서버) - 선택 사항
목적: 경비대에게 항복 요청 (수배 상태 해제 및 페널티)
데이터: int GuardId (항복하려는 경비대)

---
### 패킷 처리 흐름
수신 (Receiving):
클라이언트로부터 비동기적으로 데이터를 수신합니다.
수신된 바이트 스트림에서 패킷 경계를 식별하고 (길이 정보 활용), 완전한 패킷 데이터를 추출합니다.
역직렬화 (Deserialization):
추출된 바이트 데이터를 사전에 정의된 패킷 객체로 변환합니다.
패킷 핸들러 매핑 (Handler Mapping):
패킷 ID를 기반으로 해당 패킷을 처리할 적절한 핸들러(메서드 또는 클래스)를 찾습니다.
Dictionary&lt;PacketID, Action&lt;Session, IPacket>> 형태나, 리플렉션을 활용한 동적 매핑 등을 고려할 수 있습니다.
로직 처리 (Logic Processing):
선택된 핸들러가 패킷 데이터를 사용하여 게임 로직을 수행합니다.
이 과정에서 플레이어 상태 변경, 월드 상태 변경, DB 연동 등이 발생할 수 있습니다.
필요에 따라 다른 플레이어에게 브로드캐스팅할 패킷을 생성합니다.
응답 생성 및 직렬화 (Response Generation & Serialization):
처리 결과에 따라 클라이언트에게 응답할 패킷(들)을 생성하고, 이를 바이트 데이터로 직렬화합니다.
송신 (Sending):
직렬화된 패킷 데이터를 해당 클라이언트(들)에게 비동기적으로 송신합니다.
4. 주요 패킷 분류별 구현 가이드라인
가. 시스템 및 연결
C_AuthToken / S_AuthResult:  
AuthToken은 웹서버에서 발급된 JWT(JSON Web Token) 등을 사용할 수 있습니다. 실시간 서버는 이 토큰의 유효성을 검증합니다.
인증 성공 시 PlayerId를 내부 세션 정보에 저장하고, 클라이언트에게 전달하여 이후 통신에 활용합니다.   
C_Disconnect / S_Disconnected:  
연결 해제 사유 코드를 정의하여 클라이언트와 서버가 원인을 파악할 수 있도록 합니다.   
비정상 종료(예: 네트워크 끊김) 시에도 서버는 이를 감지하고 관련 리소스를 정리해야 합니다 (Keep-Alive 패킷 또는 소켓 상태 확인).
S_SystemMessage: 서버 공지, 점검 알림 등에 사용되며, MessageType으로 중요도를 구분합니다.   
나. 월드 및 플레이어 관리
C_EnterWorld / S_EnterWorldResult:  
월드 진입 시 플레이어의 초기 데이터(위치, 스탯, 인벤토리 등)를 로드하여 전달합니다.  이 과정은 DB 접근이 필요하므로 비동기로 처리하는 것이 좋습니다.  
S_InitialInventory  패킷을 이때 함께 보내거나, S_EnterWorldResult 내에 포함시킬 수 있습니다.   
S_SpawnPlayer, S_DespawnPlayer, S_SpawnMonster, S_DespawnMonster, S_SpawnNPC, S_DespawnNPC, S_SpawnItem, S_SpawnGuard, S_DespawnGuard:  
Area of Interest (AoI) / 시야 관리: 모든 플레이어에게 모든 엔티티 정보를 보낼 필요는 없습니다. 플레이어 주변의 일정 범위 내 엔티티 정보만 동기화하는 AoI 시스템 구현이 필수적입니다. (예: Grid 기반, Quadtree/Octree 활용)
엔티티 생성/소멸 정보는 해당 AoI 내의 클라이언트들에게 브로드캐스팅합니다.
S_SpawnPlayer 시 EquippedItems 정보 요약은 다른 플레이어의 외형을 즉시 렌더링하는 데 필요합니다.   
S_PlayerWantedStatus: GTA와 같은 자유도를 강조하는 게임이므로, 플레이어의 수배 상태는 주변 플레이어에게도 중요 정보입니다.   
다. 코어 실시간 기능
C_Move / S_Move:  
이동 동기화: 클라이언트 예측(Client-Side Prediction) 및 서버 보정(Server Reconciliation) 기법을 사용하여 부드러운 이동을 구현하는 것이 일반적입니다.
Timestamp를 활용하여 패킷 순서 보정 및 지연 시간을 감안한 처리를 할 수 있습니다.   
서버는 클라이언트의 이동 요청을 검증(속도, 충돌 등)하고, 유효한 경우 해당 정보를 AoI 내 다른 클라이언트들에게 S_Move로 브로드캐스팅합니다.
S_EntityStateChange, S_HpChange:  
엔티티의 주요 상태 변화(HP, MP, 상태 이상 등)를 관련 클라이언트에게 전달합니다.
CasterId 또는 AttackerId를 포함하여 누가 변화를 유발했는지 알 수 있도록 합니다.   
S_ReputationChange: 평판/악명 시스템은 게임의 핵심 요소이므로, 변화 시 명확한 피드백이 필요합니다.   
라. 상호작용 및 액션
C_Attack / S_AttackResult:  
서버는 공격 유효성(거리, 쿨타임, 자원 소모 등)을 판정하고, 피격 판정(Collision Detection)을 수행합니다.
공격 결과(데미지, 사망 여부 등)를 공격자와 피격자, 그리고 주변 관전자(AoI 내)에게 전달합니다.   
C_InteractObject / S_ObjectStateChange: 오브젝트의 상태 변화를 관련 클라이언트들에게 동기화합니다.  
NPC 대화 (C_TalkToNPC, S_NPCDialogue, C_DialogueResponse) 및 퀘스트 (C_QuestAction, S_QuestUpdate):  
서버는 NPC의 대화 로직, 퀘스트 상태를 관리하고 클라이언트의 선택/행동에 따라 적절히 응답합니다.
퀘스트 진행 상황은 DB에 저장되어야 하며, S_QuestUpdate를 통해 클라이언트에게 실시간으로 알립니다.   
마. 상점 및 경제
상점 아이템 목록 (C_RequestShopItems, S_ShopItemList) 및 구매/판매 로직 (C_BuyItemFromShop, S_BuyItemResult)은 서버에서 처리합니다.   
구매 시 재화 차감, 아이템 추가 등의 트랜잭션은 원자적으로 처리되어야 합니다 (DB 트랜잭션 활용).
S_CurrencyChange: 재화 변경 시 클라이언트 UI를 업데이트하도록 알립니다.   
바. 인벤토리 및 장비
S_InitialInventory: 캐릭터의 전체 인벤토리 정보를 전달합니다.   
아이템 추가/제거/사용 (S_AddItemToInventory, S_RemoveItemFromInventory, C_UseItem)은 서버에서 유효성을 검증하고 결과를 클라이언트에 알립니다.   
장비 장착/해제 (C_EquipItem, S_EquipResult, C_UnequipItem, S_UnequipResult) 역시 서버에서 처리하며, 장착 시 캐릭터 스탯 변화 등을 함께 계산합니다.   
S_PlayerAppearanceChange: 다른 플레이어에게 장비 변경으로 인한 외형 변화를 알립니다. S_SpawnPlayer의 EquippedItems와 유사한 정보를 전달할 수 있습니다.   
아이템 강화 (C_EnhanceItem, S_EnhanceItemResult): 강화 로직(성공/실패 확률, 파괴 여부, 재료 소모)은 전적으로 서버에서 수행되어야 합니다.   
사. 성장 (레벨/경험치)
S_GainExp, S_LevelUp: 경험치 획득 및 레벨 업 처리는 서버에서 이루어지며, 결과만 클라이언트에 통보합니다. 레벨 업 시 스탯 상승 등의 부가 효과도 서버에서 계산합니다.   
아. 소셜
C_Chat / S_Chat: 채팅 메시지는 서버를 경유하며, 서버는 욕설 필터링, 특정 대상에게만 보내는 귓속말 처리 등을 수행할 수 있습니다. ChatType으로 일반, 파티, 길드 채팅 등을 구분합니다.   
자. GTA5 판타지 특화 예시
C_StealTarget / S_StealResult: 훔치기 성공 여부, 발각 여부, 획득 아이템 등을 서버가 판정합니다. 발각 시 평판 하락, 경비병 스폰 등의 연계 시스템이 동작할 수 있습니다.   
C_UseSkill / S_UseSkill: 스킬 사용 조건(쿨타임, 자원) 및 효과 판정은 서버에서 담당합니다.   
탈것 (C_MountVehicle, S_MountVehicle): 탈것 탑승/하차 상태를 동기화합니다.  
S_WantedLevelChange: 수배 레벨 변경 시 플레이어 본인에게 알리고, 필요시 주변 플레이어에게도 S_PlayerWantedStatus와 같은 형태로 알릴 수 있습니다.   
C_SurrenderToGuard: 항복 시 수배 레벨 해제 및 페널티(골드 차감, 아이템 압수 등)를 서버에서 처리합니다.
