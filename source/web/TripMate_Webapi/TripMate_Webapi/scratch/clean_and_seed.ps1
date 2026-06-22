$url = "https://nvbvvowyjzylllswhynv.supabase.co"
$key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im52YnZ2b3d5anp5bGxsc3doeW52Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc3NDE2OTAzNiwiZXhwIjoyMDg5NzQ1MDM2fQ.to_NSpDGSir0FMepCjOdcV8LXSb7zxkvFLWYbtZQgIM"

$headers = @{
    "apikey" = $key
    "Authorization" = "Bearer $key"
    "Content-Type" = "application/json"
}

# 1. Clean DB
Invoke-RestMethod -Uri "$url/rest/v1/guide_profiles" -Headers $headers -Method Delete | Out-Null
Write-Host "Deleted all guide profiles"

Invoke-RestMethod -Uri "$url/rest/v1/profiles?role=eq.guide" -Headers $headers -Method Delete | Out-Null
Write-Host "Deleted all guide profiles from profiles table"

# 2. Re-seed exactly 5 guides
$guides = @(
    @{ email = "minhtuan2@tripmate.com"; name="Minh Tuấn"; bio="Food enthusiast and history buff."; avatar="https://images.unsplash.com/photo-1500648767791-00dcc994a43e?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80"; cover="https://images.unsplash.com/photo-1555939594-58d7cb561ad1?ixlib=rb-4.0.3&auto=format&fit=crop&w=1200&q=80"; city="Hanoi"; spec="Street Food, History" },
    @{ email = "linhnguyen2@tripmate.com"; name="Linh Nguyen"; bio="Professional photographer and culture guide."; avatar="https://images.unsplash.com/photo-1534528741775-53994a69daeb?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80"; cover="https://images.unsplash.com/photo-1528127269322-539801943592?ixlib=rb-4.0.3&auto=format&fit=crop&w=1200&q=80"; city="Hoi An"; spec="Photography, Nature" },
    @{ email = "hoanganh2@tripmate.com"; name="Hoàng Anh"; bio="Ready to conquer the highest peaks of Ha Giang with you."; avatar="https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80"; cover="https://images.unsplash.com/photo-1518182170546-076616fdfaaf?ixlib=rb-4.0.3&auto=format&fit=crop&w=1200&q=80"; city="Ha Giang"; spec="Trekking, Adventure" },
    @{ email = "thuytien2@tripmate.com"; name="Thủy Tiên"; bio="Experience the vibrant energy of Saigon nights on the back of my scooter!"; avatar="https://images.unsplash.com/photo-1438761681033-6461ffad8d80?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80"; cover="https://images.unsplash.com/photo-1583417319070-4a69db38a482?ixlib=rb-4.0.3&auto=format&fit=crop&w=1200&q=80"; city="Ho Chi Minh City"; spec="Nightlife, Motorbike" },
    @{ email = "guide2@tripmate.com"; name="TripMate Guide"; bio="Experienced TripMate tour guide."; avatar="https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80"; cover="https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?ixlib=rb-4.0.3&auto=format&fit=crop&w=1200&q=80"; city="Ho Chi Minh City"; spec="Local Expert, Culture" }
)

$usersRes = Invoke-RestMethod -Uri "$url/auth/v1/admin/users" -Headers $headers -Method Get

foreach ($g in $guides) {
    $user = $usersRes.users | Where-Object { $_.email -eq $g.email }
    if (-not $user) {
        $userPayload = @{ email = $g.email; password = "password123"; email_confirm = $true } | ConvertTo-Json
        try {
            $adminRes = Invoke-RestMethod -Uri "$url/auth/v1/admin/users" -Headers $headers -Method Post -Body $userPayload
            $userId = $adminRes.id
            Write-Host "Created admin user: $($g.email)"
        } catch {
            Write-Host "Failed to create admin user: $($g.email). Error: $_"
            continue
        }
    } else {
        $userId = $user.id
    }

    $profilePayload = @{
        id = $userId
        email = $g.email
        full_name = $g.name
        role = "guide"
        is_active = $true
        avatar_url = $g.avatar
        created_at = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        updated_at = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    } | ConvertTo-Json
    $headersWithMerge = $headers.Clone()
    $headersWithMerge["Prefer"] = "resolution=merge-duplicates"
    Invoke-RestMethod -Uri "$url/rest/v1/profiles" -Headers $headersWithMerge -Method Post -Body $profilePayload | Out-Null
    Write-Host "Upserted profile: $($g.email)"

    $specs = @()
    foreach ($s in $g.spec.Split(',')) { $specs += $s.Trim() }

    $guidePayload = @{
        id = [guid]::NewGuid().ToString()
        user_id = $userId
        bio = $g.bio
        city_area = $g.city
        specialties = $specs
        languages = @("Vietnamese", "English")
        price_per_hour = 250000
        is_verified = $true
        verified_at = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        average_rating = 4.8
        total_reviews = 42
        cover_photo_url = $g.cover
        created_at = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        updated_at = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    } | ConvertTo-Json -Depth 10

    Invoke-RestMethod -Uri "$url/rest/v1/guide_profiles" -Headers $headersWithMerge -Method Post -Body $guidePayload | Out-Null
    Write-Host "Upserted guide profile: $($g.email)"
}
