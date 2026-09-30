$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$port = 8091
$prefix = "http://127.0.0.1:$port/"

$types = @{
    ".html" = "text/html; charset=utf-8"
    ".js"   = "text/javascript; charset=utf-8"
    ".css"  = "text/css; charset=utf-8"
    ".svg"  = "image/svg+xml"
    ".json" = "application/json"
    ".png"  = "image/png"
    ".jpg"  = "image/jpeg"
    ".jpeg" = "image/jpeg"
    ".webp" = "image/webp"
    ".gif"  = "image/gif"
    ".mp3"  = "audio/mpeg"
    ".glb"  = "model/gltf-binary"
    ".txt"  = "text/plain; charset=utf-8"
}

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($prefix)
try {
    $listener.Start()
} catch {
    Write-Host "$port 已经被占用。关掉之前的窗口，或改 serve.ps1 里的 port。"
    exit 1
}

$url = "${prefix}index.html"
Write-Host "游戏开在 $url"
Write-Host "关掉这个窗口就停。"
Start-Process $url

while ($listener.IsListening) {
    $ctx = $listener.GetContext()
    try {
        $rel = [Uri]::UnescapeDataString($ctx.Request.Url.LocalPath).TrimStart("/")
        if ([string]::IsNullOrWhiteSpace($rel)) { $rel = "index.html" }
        $file = Join-Path $root ($rel -replace "/", "\")
        $rootFull = [IO.Path]::GetFullPath($root)
        $fileFull = [IO.Path]::GetFullPath($file)
        if ($fileFull.StartsWith($rootFull, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path $fileFull -PathType Leaf)) {
            $ext = [IO.Path]::GetExtension($fileFull).ToLower()
            $ctx.Response.ContentType = $(if ($types.ContainsKey($ext)) { $types[$ext] } else { "application/octet-stream" })
            $ctx.Response.Headers["Cache-Control"] = "no-store"
            $bytes = [IO.File]::ReadAllBytes($fileFull)
            $ctx.Response.ContentLength64 = $bytes.Length
            $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length)
        } else {
            $ctx.Response.StatusCode = 404
        }
    } catch {
    } finally {
        try { $ctx.Response.OutputStream.Close() } catch {}
    }
}
