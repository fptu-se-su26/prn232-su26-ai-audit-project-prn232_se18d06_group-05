$url = "https://nvbvvowyjzylllswhynv.supabase.co"
$key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im52YnZ2b3d5anp5bGxsc3doeW52Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc3NDE2OTAzNiwiZXhwIjoyMDg5NzQ1MDM2fQ.to_NSpDGSir0FMepCjOdcV8LXSb7zxkvFLWYbtZQgIM"

$headers = @{
    "apikey" = $key
    "Authorization" = "Bearer $key"
    "Content-Type" = "application/json"
    "Prefer" = "resolution=merge-duplicates"
}

# Fetch guides from profiles
$profiles = Invoke-RestMethod -Uri "$url/rest/v1/profiles?role=eq.guide" -Headers $headers -Method Get

if ($profiles.Count -eq 0) {
    Write-Host "No guides found in profiles table."
    exit
}

foreach ($p in $profiles) {
    Write-Host "Found guide: $($p.full_name) ($($p.id))"
    
    $payload = @{
        "id" = [guid]::NewGuid().ToString()
        "user_id" = $p.id
        "bio" = "Passionate local expert ready to show you the best of Vietnam."
        "languages" = @("English", "Vietnamese")
        "specialties" = @("Culture", "Food")
        "city_area" = "Hanoi"
        "price_per_hour" = 250000
        "is_verified" = $true
        "verified_at" = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        "average_rating" = 4.8
        "total_reviews" = 42
    } | ConvertTo-Json -Depth 10

    try {
        Invoke-RestMethod -Uri "$url/rest/v1/guide_profiles" -Headers $headers -Method Post -Body $payload | Out-Null
        Write-Host "Successfully inserted guide profile."
    } catch {
        Write-Host "Failed: $_"
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response Body: $responseBody"
        }
    }
}
