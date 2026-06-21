// 브라우저용 RTW 웹 클라이언트를 위한 의존성 0 개발 서버.
//
// 두 가지 일을 한다:
//   1) 정적 파일 서빙 — .ts 요청이 오면 Node 내장 stripTypeScriptTypes로 타입을 벗겨
//      브라우저가 그대로 import 할 수 있는 JS로 변환해 돌려준다(빌드 단계 없음).
//      덕분에 src/api.ts, src/types.ts를 브라우저에서 "그대로" 재사용한다.
//   2) API 프록시 — 정적 경로가 아닌 모든 요청(/Account, /Game, /Mission ...)을
//      백엔드 RTWWebServer로 그대로 흘려보낸다. 같은 오리진이 되므로 서버 CORS 설정이 필요 없다.
//
// 실행:  node web/serve.ts
//        PORT=8080  RTW_BASE_URL=http://localhost:5080  node web/serve.ts

import { createServer, type IncomingMessage, type ServerResponse } from "node:http";
import { readFile } from "node:fs/promises";
import { stripTypeScriptTypes } from "node:module";
import { extname, join, normalize, sep } from "node:path";
import { env, exit } from "node:process";

const ROOT = join(import.meta.dirname, ".."); // ts-client/
const PORT = Number(env.PORT ?? 8080);
const BACKEND = (env.RTW_BASE_URL ?? "http://localhost:5080").replace(/\/+$/, "");

const MIME: Record<string, string> = {
  ".html": "text/html; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".ts": "text/javascript; charset=utf-8", // 타입 제거 후 JS로 서빙
  ".json": "application/json; charset=utf-8",
  ".svg": "image/svg+xml",
  ".ico": "image/x-icon",
};

// 정적 자산 경로인가? (아니면 백엔드로 프록시한다)
function isStatic(pathname: string): boolean {
  return pathname === "/" || pathname.startsWith("/web/") || pathname.startsWith("/src/");
}

async function serveStatic(pathname: string, res: ServerResponse): Promise<void> {
  const rel = pathname === "/" ? "/web/index.html" : pathname;
  const filePath = normalize(join(ROOT, rel));

  // 경로 탈출 방지: ROOT 밖이면 거부
  if (filePath !== ROOT && !filePath.startsWith(ROOT + sep)) {
    res.writeHead(403).end("Forbidden");
    return;
  }

  let buf: Buffer;
  try {
    buf = await readFile(filePath);
  } catch {
    res.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" }).end(`Not found: ${pathname}`);
    return;
  }

  const ext = extname(filePath);
  if (ext === ".ts") {
    // erasable-only TS만 다루므로 mode "strip"으로 충분(소스맵 불필요).
    const js = stripTypeScriptTypes(buf.toString("utf8"), { mode: "strip" });
    res.writeHead(200, { "Content-Type": MIME[".ts"] }).end(js);
    return;
  }

  res.writeHead(200, { "Content-Type": MIME[ext] ?? "application/octet-stream" }).end(buf);
}

async function proxy(req: IncomingMessage, res: ServerResponse, pathname: string, search: string): Promise<void> {
  const chunks: Buffer[] = [];
  for await (const c of req) chunks.push(c as Buffer);
  const body = chunks.length > 0 ? Buffer.concat(chunks) : undefined;

  // 인증/콘텐츠 헤더만 선별 전달한다.
  const headers: Record<string, string> = {};
  for (const h of ["content-type", "accept", "authorization", "x-user-id", "x-auth-token"]) {
    const v = req.headers[h];
    if (typeof v === "string") headers[h] = v;
  }

  const method = req.method ?? "GET";
  let backendRes: Response;
  try {
    backendRes = await fetch(`${BACKEND}${pathname}${search}`, {
      method,
      headers,
      body: method === "GET" || method === "HEAD" ? undefined : body,
    });
  } catch (e) {
    res
      .writeHead(502, { "Content-Type": "application/json; charset=utf-8" })
      .end(JSON.stringify({ errorCode: 1999, proxyError: `백엔드 연결 실패 (${BACKEND}): ${(e as Error).message}` }));
    return;
  }

  const respBuf = Buffer.from(await backendRes.arrayBuffer());
  const ct = backendRes.headers.get("content-type") ?? "application/json; charset=utf-8";
  res.writeHead(backendRes.status, { "Content-Type": ct }).end(respBuf);
}

const server = createServer((req, res) => {
  const url = new URL(req.url ?? "/", `http://localhost:${PORT}`);
  if (url.pathname === "/favicon.ico") {
    res.writeHead(204).end();
    return;
  }
  const handler = isStatic(url.pathname)
    ? serveStatic(url.pathname, res)
    : proxy(req, res, url.pathname, url.search);
  handler.catch((e) => {
    if (!res.headersSent) res.writeHead(500, { "Content-Type": "text/plain; charset=utf-8" });
    res.end(`Server error: ${(e as Error).message}`);
  });
});

server.on("error", (e: Error & { code?: string }) => {
  if (e.code === "EADDRINUSE") {
    console.error(`\n  ✗ 포트 ${PORT}이(가) 이미 사용 중입니다.`);
    console.error(`    다른 포트로 실행:   PORT=8090 npm run web`);
    console.error(`    점유 프로세스 확인:  lsof -i :${PORT}\n`);
    exit(1);
  }
  throw e;
});

server.listen(PORT, () => {
  console.log(`\n  RTW 웹 클라이언트  →  http://localhost:${PORT}`);
  console.log(`  백엔드 프록시       →  ${BACKEND}`);
  console.log(`\n  (백엔드 주소가 다르면)  RTW_BASE_URL=http://localhost:5080 node web/serve.ts\n`);
});
