$url = "https://nvbvvowyjzylllswhynv.supabase.co"
$key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im52YnZ2b3d5anp5bGxsc3doeW52Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc3NDE2OTAzNiwiZXhwIjoyMDg5NzQ1MDM2fQ.to_NSpDGSir0FMepCjOdcV8LXSb7zxkvFLWYbtZQgIM"

$headers = @{
    "apikey" = $key
    "Authorization" = "Bearer $key"
    "Content-Type" = "application/json"
    "Prefer" = "resolution=merge-duplicates"
}

# 1. Get first guide
$guides = Invoke-RestMethod -Uri "$url/rest/v1/guide_profiles?limit=1" -Headers $headers -Method Get

if ($guides.Count -gt 0) {
    $guideId = $guides[0].id
    Write-Host "Found Guide ID: $guideId"

    # 2. Insert dummy package
    $pkgPayload = @{
        id = "00000000-0000-0000-0000-000000000000"
        guide_profile_id = $guideId
        title = "Custom Itinerary"
        description = "A personalized tour based on your preferences."
        duration_hours = 4
        price_per_session = 0
        price_per_person = 500000
        max_group_size = 10
        is_active = $true
        created_at = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        updated_at = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    } | ConvertTo-Json

    Invoke-RestMethod -Uri "$url/rest/v1/experience_packages" -Headers $headers -Method Post -Body $pkgPayload | Out-Null
    Write-Host "Inserted dummy package 00000000-0000-0000-0000-000000000000 successfully."
} else {
    Write-Host "No guides found, cannot insert dummy package."
}
