$url = "https://nvbvvowyjzylllswhynv.supabase.co"
$key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im52YnZ2b3d5anp5bGxsc3doeW52Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc3NDE2OTAzNiwiZXhwIjoyMDg5NzQ1MDM2fQ.to_NSpDGSir0FMepCjOdcV8LXSb7zxkvFLWYbtZQgIM"

$headers = @{
    "apikey" = $key
    "Authorization" = "Bearer $key"
    "Content-Type" = "application/json"
}

try {
    $response = Invoke-RestMethod -Uri "$url/rest/v1/guide_profiles?select=*,profiles(*)" -Headers $headers -Method Get
    Write-Host "Success:"
    $response | ConvertTo-Json -Depth 10
} catch {
    Write-Host "Failed: $_"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody"
    }
}
