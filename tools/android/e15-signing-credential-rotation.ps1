function Get-E15SigningCredentialRotationPolicy {
    return [pscustomobject]@{
        incidentCutoffUtc = [DateTimeOffset]::Parse("2026-08-31T05:07:26Z")
        certificateSha256 = "204C558297B3ACA278537D3F02794F87965E5CC2684FB5A7E563A9C4565894D7"
        credentialVerification = "keytool-env-list"
        retiredKeystoreSha256 = "8ED8A7640A37D7063222C8245B2427E63C9DE3D9BC6A0FC9DF45056277BB37C1"
        retiredCredentialFileSha256 = "FE0F463BD5BD984322D33A2A0B4D49B2A6EA46B2AFBEE0E89DF171DA0A58F198"
    }
}

function Test-E15SigningPathInsideDirectory {
    param(
        [string]$Path,
        [string]$Directory
    )

    if (-not $Path -or -not $Directory) {
        return $false
    }
    $root = [IO.Path]::GetFullPath($Directory).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $candidate = [IO.Path]::GetFullPath($Path)
    return $candidate.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)
}

function Get-E15SigningCredentialBundle {
    [CmdletBinding()]
    param(
        [string]$CredentialPath,
        [string]$ExpectedKeyAlias,
        [DateTimeOffset]$CurrentUtc = [DateTimeOffset]::UtcNow
    )

    $policy = Get-E15SigningCredentialRotationPolicy
    $reasons = New-Object System.Collections.Generic.List[string]
    $resolvedCredential = ""
    if ([string]::IsNullOrWhiteSpace($CredentialPath)) {
        $reasons.Add("E15 signing DPAPI bundle path is missing.")
    }
    else {
        try {
            $resolvedCredential = [IO.Path]::GetFullPath($CredentialPath)
            if (-not (Test-Path -LiteralPath $resolvedCredential -PathType Leaf)) {
                $reasons.Add("E15 signing DPAPI bundle is missing: $resolvedCredential")
            }
        }
        catch {
            $reasons.Add("E15 signing DPAPI bundle path is invalid.")
        }
    }

    $bundle = $null
    if ($resolvedCredential -and (Test-Path -LiteralPath $resolvedCredential -PathType Leaf)) {
        try {
            $bundle = Import-Clixml -LiteralPath $resolvedCredential
        }
        catch {
            $reasons.Add("E15 signing DPAPI bundle could not be decrypted by the current Windows user.")
        }
    }

    $createdAt = [DateTimeOffset]::MinValue
    if ($null -ne $bundle) {
        $requiredProperties = @(
            "schemaVersion",
            "state",
            "createdAtUtc",
            "keyAlias",
            "keystorePassword",
            "keyPassword"
        )
        $missingProperties = @($requiredProperties | Where-Object {
            $null -eq $bundle.PSObject.Properties[$_]
        })
        if ($missingProperties.Count -gt 0) {
            $reasons.Add("E15 signing DPAPI bundle schema is incomplete: $($missingProperties -join ', ').")
        }
        else {
            $schemaVersion = 0
            $schemaVersionValid = [int]::TryParse([string]$bundle.schemaVersion, [ref]$schemaVersion)
            if (-not $schemaVersionValid `
                -or $schemaVersion -ne 1 `
                -or [string]$bundle.state -cne "signing_credential_ready") {
                $reasons.Add("E15 signing DPAPI bundle state or schema is invalid.")
            }
            if ([string]$bundle.keyAlias -cne $ExpectedKeyAlias) {
                $reasons.Add("E15 signing DPAPI bundle alias does not match the selected key alias.")
            }
            if ($bundle.keystorePassword -isnot [System.Security.SecureString] `
                -or $bundle.keystorePassword.Length -eq 0) {
                $reasons.Add("E15 signing DPAPI bundle keystore password is missing or not a SecureString.")
            }
            if ($bundle.keyPassword -isnot [System.Security.SecureString] `
                -or $bundle.keyPassword.Length -eq 0) {
                $reasons.Add("E15 signing DPAPI bundle key password is missing or not a SecureString.")
            }
            $timestampValid = [DateTimeOffset]::TryParse(
                [string]$bundle.createdAtUtc,
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::RoundtripKind,
                [ref]$createdAt)
            if (-not $timestampValid `
                -or $createdAt.Offset -ne [TimeSpan]::Zero `
                -or $createdAt -le $policy.incidentCutoffUtc `
                -or $createdAt -gt $CurrentUtc.ToUniversalTime().AddMinutes(5)) {
                $reasons.Add("E15 signing DPAPI bundle timestamp must be UTC, post-incident, and not in the future.")
            }
        }
    }

    return [pscustomobject]@{
        passed = $reasons.Count -eq 0
        reasons = $reasons.ToArray()
        path = $resolvedCredential
        sha256 = if ($resolvedCredential -and (Test-Path -LiteralPath $resolvedCredential -PathType Leaf)) {
            (Get-FileHash -LiteralPath $resolvedCredential -Algorithm SHA256).Hash
        }
        else {
            ""
        }
        keyAlias = if ($null -ne $bundle -and $null -ne $bundle.PSObject.Properties["keyAlias"]) {
            [string]$bundle.keyAlias
        }
        else {
            ""
        }
        keystorePassword = if ($null -ne $bundle `
            -and $null -ne $bundle.PSObject.Properties["keystorePassword"] `
            -and $bundle.keystorePassword -is [System.Security.SecureString]) {
            $bundle.keystorePassword
        }
        else {
            $null
        }
        keyPassword = if ($null -ne $bundle `
            -and $null -ne $bundle.PSObject.Properties["keyPassword"] `
            -and $bundle.keyPassword -is [System.Security.SecureString]) {
            $bundle.keyPassword
        }
        else {
            $null
        }
    }
}

function New-E15SigningCredentialBundle {
    [CmdletBinding()]
    param(
        [string]$OutputPath,
        [string]$KeyAlias,
        [System.Security.SecureString]$KeystorePassword,
        [System.Security.SecureString]$KeyPassword,
        [string]$RepositoryRoot,
        [DateTimeOffset]$CreatedAtUtc = [DateTimeOffset]::UtcNow
    )

    foreach ($requiredInput in @(
        [pscustomobject]@{ name = "output path"; value = $OutputPath },
        [pscustomobject]@{ name = "key alias"; value = $KeyAlias },
        [pscustomobject]@{ name = "repository root"; value = $RepositoryRoot })) {
        if ([string]::IsNullOrWhiteSpace($requiredInput.value)) {
            throw "E15 signing DPAPI bundle $($requiredInput.name) is missing."
        }
    }
    if ($KeyAlias -notmatch '^[A-Za-z0-9._-]+$') {
        throw "E15 signing DPAPI bundle key alias contains unsupported characters."
    }
    if ($null -eq $KeystorePassword -or $KeystorePassword.Length -eq 0 `
        -or $null -eq $KeyPassword -or $KeyPassword.Length -eq 0) {
        throw "E15 signing DPAPI bundle requires non-empty store and key SecureStrings."
    }

    $resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
    if (Test-E15SigningPathInsideDirectory -Path $resolvedOutput -Directory $RepositoryRoot) {
        throw "E15 signing DPAPI bundle must remain outside the repository: $resolvedOutput"
    }
    if (Test-Path -LiteralPath $resolvedOutput) {
        throw "Refusing to overwrite an existing E15 signing DPAPI bundle: $resolvedOutput"
    }
    $parent = [IO.Path]::GetDirectoryName($resolvedOutput)
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    [pscustomobject]@{
        schemaVersion = 1
        state = "signing_credential_ready"
        createdAtUtc = $CreatedAtUtc.ToUniversalTime().ToString("o")
        keyAlias = $KeyAlias
        keystorePassword = $KeystorePassword
        keyPassword = $KeyPassword
    } | Export-Clixml -LiteralPath $resolvedOutput

    $validation = Get-E15SigningCredentialBundle `
        -CredentialPath $resolvedOutput `
        -ExpectedKeyAlias $KeyAlias
    if (-not [bool]$validation.passed) {
        Remove-Item -LiteralPath $resolvedOutput -Force
        throw "Generated E15 signing DPAPI bundle failed validation: $($validation.reasons -join ' ')"
    }
    return $validation
}

function Test-E15SigningCredentialAccess {
    [CmdletBinding()]
    param(
        [string]$KeystorePath,
        [string]$CredentialPath,
        [string]$KeyAlias,
        [string]$KeytoolPath,
        [string]$ExpectedCertificateSha256,
        [string]$ExpectedKeytoolSha256 = ""
    )

    $reasons = New-Object System.Collections.Generic.List[string]
    $resolvedKeystore = ""
    $resolvedCredential = ""
    $resolvedKeytool = ""
    foreach ($inputPath in @(
        [pscustomobject]@{ name = "keystore"; key = "keystore"; value = $KeystorePath },
        [pscustomobject]@{ name = "DPAPI credential"; key = "credential"; value = $CredentialPath },
        [pscustomobject]@{ name = "keytool"; key = "keytool"; value = $KeytoolPath })) {
        if ([string]::IsNullOrWhiteSpace($inputPath.value)) {
            $reasons.Add("E15 signing credential access $($inputPath.name) path is missing.")
            continue
        }
        try {
            $fullPath = [IO.Path]::GetFullPath($inputPath.value)
            switch ($inputPath.key) {
                "keystore" { $resolvedKeystore = $fullPath }
                "credential" { $resolvedCredential = $fullPath }
                "keytool" { $resolvedKeytool = $fullPath }
            }
            if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
                $reasons.Add("E15 signing credential access $($inputPath.name) file is missing: $fullPath")
            }
        }
        catch {
            $reasons.Add("E15 signing credential access $($inputPath.name) path is invalid.")
        }
    }
    if ([string]::IsNullOrWhiteSpace($KeyAlias) -or $KeyAlias -notmatch '^[A-Za-z0-9._-]+$') {
        $reasons.Add("E15 signing credential access key alias is missing or contains unsupported characters.")
    }
    if ($ExpectedCertificateSha256 -notmatch '^[0-9A-Fa-f]{64}$') {
        $reasons.Add("E15 signing credential access expected certificate fingerprint is invalid.")
    }
    $keytoolSha256 = if ($resolvedKeytool -and (Test-Path -LiteralPath $resolvedKeytool -PathType Leaf)) {
        (Get-FileHash -LiteralPath $resolvedKeytool -Algorithm SHA256).Hash
    }
    else {
        ""
    }
    if ($ExpectedKeytoolSha256 `
        -and ($ExpectedKeytoolSha256 -notmatch '^[0-9A-Fa-f]{64}$' -or $keytoolSha256 -ine $ExpectedKeytoolSha256)) {
        $reasons.Add("E15 signing keytool binary does not match the rotation record.")
    }

    $credentialBundle = Get-E15SigningCredentialBundle `
        -CredentialPath $resolvedCredential `
        -ExpectedKeyAlias $KeyAlias
    if (-not [bool]$credentialBundle.passed) {
        foreach ($reason in $credentialBundle.reasons) {
            $reasons.Add($reason)
        }
    }

    $certificateSha256 = ""
    if ($reasons.Count -eq 0) {
        $passwordPointer = [IntPtr]::Zero
        $passwordPlain = $null
        $process = $null
        $startInfo = $null
        $passwordVariableName = ""
        try {
            $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($credentialBundle.keystorePassword)
            $passwordPlain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
            $startInfo = New-Object System.Diagnostics.ProcessStartInfo
            $startInfo.FileName = $resolvedKeytool
            $passwordVariableName = "CGE15_KEYTOOL_PASS_" + [guid]::NewGuid().ToString("N").ToUpperInvariant()
            $startInfo.Arguments = "-list -v -keystore `"$resolvedKeystore`" -alias `"$KeyAlias`" -storepass:env $passwordVariableName"
            $startInfo.UseShellExecute = $false
            $startInfo.CreateNoWindow = $true
            $startInfo.RedirectStandardOutput = $true
            $startInfo.RedirectStandardError = $true
            foreach ($inheritedPasswordVariable in @($startInfo.EnvironmentVariables.Keys) | Where-Object {
                $_ -in @("CATGUARD_ANDROID_KEYSTORE_PASSWORD", "CATGUARD_ANDROID_KEY_PASSWORD") `
                    -or $_ -like "CGE15_KEYTOOL_PASS_*"
            }) {
                $startInfo.EnvironmentVariables.Remove($inheritedPasswordVariable)
            }
            $startInfo.EnvironmentVariables[$passwordVariableName] = $passwordPlain
            $process = New-Object System.Diagnostics.Process
            $process.StartInfo = $startInfo
            if (-not $process.Start()) {
                throw "keytool did not start."
            }
            $startInfo.EnvironmentVariables.Remove($passwordVariableName)
            $passwordPlain = $null
            $stdoutTask = $process.StandardOutput.ReadToEndAsync()
            $stderrTask = $process.StandardError.ReadToEndAsync()
            $process.WaitForExit()
            $output = $stdoutTask.Result + [Environment]::NewLine + $stderrTask.Result
            if ($process.ExitCode -ne 0) {
                $reasons.Add("E15 signing DPAPI credential could not open the selected keystore alias through keytool.")
            }
            else {
                $fingerprintMatch = [regex]::Match($output, '(?im)^\s*SHA256:\s*([0-9A-F:]{95})\s*$')
                if (-not $fingerprintMatch.Success) {
                    $reasons.Add("E15 signing keytool output did not contain a SHA-256 certificate fingerprint.")
                }
                else {
                    $certificateSha256 = $fingerprintMatch.Groups[1].Value.Replace(":", "").ToUpperInvariant()
                    if ($certificateSha256 -ine $ExpectedCertificateSha256) {
                        $reasons.Add("E15 signing keystore certificate fingerprint does not match the expected upload key.")
                    }
                }
            }
        }
        catch {
            $reasons.Add("E15 signing keytool verification failed without exposing its output.")
        }
        finally {
            $passwordPlain = $null
            if ($null -ne $startInfo -and $passwordVariableName) {
                $startInfo.EnvironmentVariables.Remove($passwordVariableName)
            }
            if ($passwordPointer -ne [IntPtr]::Zero) {
                [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
            }
            if ($null -ne $process) {
                $process.Dispose()
            }
        }
    }

    return [pscustomobject]@{
        passed = $reasons.Count -eq 0
        reasons = $reasons.ToArray()
        certificateSha256 = $certificateSha256
        keytoolSha256 = $keytoolSha256
    }
}

function Test-E15SigningCredentialRotationRecord {
    [CmdletBinding()]
    param(
        [string]$RecordPath,
        [string]$KeystorePath,
        [string]$CredentialPath,
        [string]$KeyAlias,
        [string]$RepositoryRoot,
        [DateTimeOffset]$CurrentUtc = [DateTimeOffset]::UtcNow
    )

    $policy = Get-E15SigningCredentialRotationPolicy
    $reasons = New-Object System.Collections.Generic.List[string]
    $resolved = [ordered]@{
        record = ""
        keystore = ""
        credential = ""
    }
    foreach ($inputPath in @(
        [pscustomobject]@{ name = "rotation record"; key = "record"; path = $RecordPath },
        [pscustomobject]@{ name = "keystore"; key = "keystore"; path = $KeystorePath },
        [pscustomobject]@{ name = "DPAPI credential"; key = "credential"; path = $CredentialPath })) {
        if ([string]::IsNullOrWhiteSpace($inputPath.path)) {
            $reasons.Add("E15 signing $($inputPath.name) path is missing.")
            continue
        }
        try {
            $resolved[$inputPath.key] = [IO.Path]::GetFullPath($inputPath.path)
        }
        catch {
            $reasons.Add("E15 signing $($inputPath.name) path is invalid.")
            continue
        }
        if (-not (Test-Path -LiteralPath $resolved[$inputPath.key] -PathType Leaf)) {
            $reasons.Add("E15 signing $($inputPath.name) file is missing: $($resolved[$inputPath.key])")
        }
        if ($RepositoryRoot `
            -and (Test-E15SigningPathInsideDirectory -Path $resolved[$inputPath.key] -Directory $RepositoryRoot)) {
            $reasons.Add("E15 signing $($inputPath.name) must remain outside the repository.")
        }
    }

    if ([string]::IsNullOrWhiteSpace($KeyAlias)) {
        $reasons.Add("E15 signing key alias is missing.")
    }

    $actualKeystoreSha256 = if ($resolved.keystore -and (Test-Path -LiteralPath $resolved.keystore -PathType Leaf)) {
        (Get-FileHash -LiteralPath $resolved.keystore -Algorithm SHA256).Hash
    }
    else {
        ""
    }
    $actualCredentialSha256 = if ($resolved.credential -and (Test-Path -LiteralPath $resolved.credential -PathType Leaf)) {
        (Get-FileHash -LiteralPath $resolved.credential -Algorithm SHA256).Hash
    }
    else {
        ""
    }
    if ($actualKeystoreSha256 -ieq $policy.retiredKeystoreSha256) {
        $reasons.Add("E15 signing keystore still has the retired pre-rotation fingerprint.")
    }
    if ($actualCredentialSha256 -ieq $policy.retiredCredentialFileSha256) {
        $reasons.Add("E15 DPAPI credential still has the retired pre-rotation fingerprint.")
    }

    $record = $null
    $recordJson = ""
    $rotatedAtText = ""
    if ($resolved.record -and (Test-Path -LiteralPath $resolved.record -PathType Leaf)) {
        try {
            $recordJson = Get-Content -LiteralPath $resolved.record -Encoding UTF8 -Raw
            $record = $recordJson | ConvertFrom-Json
            $rotatedAtMatch = [regex]::Match(
                $recordJson,
                '(?m)"rotatedAtUtc"\s*:\s*"([^"]+)"')
            if ($rotatedAtMatch.Success) {
                $rotatedAtText = $rotatedAtMatch.Groups[1].Value
            }
        }
        catch {
            $reasons.Add("E15 signing rotation record is not valid JSON: $($_.Exception.Message)")
        }
    }

    $rotatedAt = [DateTimeOffset]::MinValue
    if ($null -ne $record) {
        $requiredProperties = @(
            "schemaVersion",
            "state",
            "passed",
            "rotatedAtUtc",
            "keyAlias",
            "certificateSha256",
            "keystoreSha256",
            "credentialFileSha256",
            "credentialVerification",
            "keytoolSha256"
        )
        $missingProperties = @($requiredProperties | Where-Object {
            $null -eq $record.PSObject.Properties[$_]
        })
        if ($missingProperties.Count -gt 0) {
            $reasons.Add("E15 signing rotation record schema is incomplete: $($missingProperties -join ', ').")
        }
        else {
            if ([int]$record.schemaVersion -ne 2 `
                -or $record.state -ne "credential_rotated" `
                -or $record.passed -isnot [bool] `
                -or -not $record.passed) {
                $reasons.Add("E15 signing rotation record does not represent a completed credential rotation.")
            }
            $timestampValid = [DateTimeOffset]::TryParse(
                $rotatedAtText,
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::RoundtripKind,
                [ref]$rotatedAt)
            if (-not $timestampValid `
                -or $rotatedAt.Offset -ne [TimeSpan]::Zero `
                -or $rotatedAt -le $policy.incidentCutoffUtc `
                -or $rotatedAt -gt $CurrentUtc.ToUniversalTime().AddMinutes(5)) {
                $reasons.Add("E15 signing rotation timestamp must be UTC, after the incident cutoff, and not in the future.")
            }
            if ([string]$record.keyAlias -cne $KeyAlias) {
                $reasons.Add("E15 signing rotation record key alias does not match the selected alias.")
            }
            if ([string]$record.certificateSha256 -ine $policy.certificateSha256) {
                $reasons.Add("E15 signing rotation record certificate fingerprint does not match the approved candidate key.")
            }
            if ([string]$record.credentialVerification -cne $policy.credentialVerification) {
                $reasons.Add("E15 signing rotation record credential verification method is invalid.")
            }
            if ([string]$record.keytoolSha256 -notmatch '^[0-9A-Fa-f]{64}$') {
                $reasons.Add("E15 signing rotation record keytool hash is invalid.")
            }
            if ([string]$record.keystoreSha256 -notmatch '^[0-9A-Fa-f]{64}$' `
                -or [string]$record.keystoreSha256 -ine $actualKeystoreSha256) {
                $reasons.Add("E15 signing rotation record keystore hash does not match the selected file.")
            }
            if ([string]$record.credentialFileSha256 -notmatch '^[0-9A-Fa-f]{64}$' `
                -or [string]$record.credentialFileSha256 -ine $actualCredentialSha256) {
                $reasons.Add("E15 signing rotation record credential hash does not match the selected DPAPI file.")
            }
            if ([string]$record.keystoreSha256 -ieq $policy.retiredKeystoreSha256) {
                $reasons.Add("E15 signing rotation record references the retired keystore fingerprint.")
            }
            if ([string]$record.credentialFileSha256 -ieq $policy.retiredCredentialFileSha256) {
                $reasons.Add("E15 signing rotation record references the retired DPAPI credential fingerprint.")
            }
        }
    }

    $recordSha256 = if ($resolved.record -and (Test-Path -LiteralPath $resolved.record -PathType Leaf)) {
        (Get-FileHash -LiteralPath $resolved.record -Algorithm SHA256).Hash
    }
    else {
        ""
    }
    return [pscustomobject]@{
        passed = $reasons.Count -eq 0
        reasons = $reasons.ToArray()
        recordPath = $resolved.record
        recordSha256 = $recordSha256
        keystoreSha256 = $actualKeystoreSha256
        credentialFileSha256 = $actualCredentialSha256
        certificateSha256 = $policy.certificateSha256
        credentialVerification = if ($null -ne $record -and $null -ne $record.PSObject.Properties["credentialVerification"]) {
            [string]$record.credentialVerification
        }
        else {
            ""
        }
        keytoolSha256 = if ($null -ne $record -and $null -ne $record.PSObject.Properties["keytoolSha256"]) {
            [string]$record.keytoolSha256
        }
        else {
            ""
        }
        rotatedAtUtc = if ($rotatedAt -ne [DateTimeOffset]::MinValue) { $rotatedAt.ToUniversalTime().ToString("o") } else { "" }
        evidence = $record
    }
}

function New-E15SigningCredentialRotationRecord {
    [CmdletBinding()]
    param(
        [string]$OutputPath,
        [string]$KeystorePath,
        [string]$CredentialPath,
        [string]$KeyAlias,
        [string]$RepositoryRoot,
        [string]$KeytoolPath,
        [DateTimeOffset]$RotatedAtUtc = [DateTimeOffset]::UtcNow
    )

    foreach ($requiredInput in @(
        [pscustomobject]@{ name = "output path"; value = $OutputPath },
        [pscustomobject]@{ name = "keystore path"; value = $KeystorePath },
        [pscustomobject]@{ name = "DPAPI credential path"; value = $CredentialPath },
        [pscustomobject]@{ name = "repository root"; value = $RepositoryRoot },
        [pscustomobject]@{ name = "keytool path"; value = $KeytoolPath })) {
        if ([string]::IsNullOrWhiteSpace($requiredInput.value)) {
            throw "E15 signing rotation $($requiredInput.name) is missing."
        }
    }

    $policy = Get-E15SigningCredentialRotationPolicy
    $resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
    $resolvedKeystore = [IO.Path]::GetFullPath($KeystorePath)
    $resolvedCredential = [IO.Path]::GetFullPath($CredentialPath)
    foreach ($path in @($resolvedOutput, $resolvedKeystore, $resolvedCredential)) {
        if (Test-E15SigningPathInsideDirectory -Path $path -Directory $RepositoryRoot) {
            throw "E15 signing rotation material must remain outside the repository: $path"
        }
    }
    if (Test-Path -LiteralPath $resolvedOutput) {
        throw "Refusing to overwrite an existing E15 signing rotation record: $resolvedOutput"
    }
    foreach ($inputPath in @($resolvedKeystore, $resolvedCredential)) {
        if (-not (Test-Path -LiteralPath $inputPath -PathType Leaf)) {
            throw "E15 signing rotation input is missing: $inputPath"
        }
    }
    if ([string]::IsNullOrWhiteSpace($KeyAlias)) {
        throw "E15 signing key alias is missing."
    }

    $keystoreSha256 = (Get-FileHash -LiteralPath $resolvedKeystore -Algorithm SHA256).Hash
    $credentialSha256 = (Get-FileHash -LiteralPath $resolvedCredential -Algorithm SHA256).Hash
    if ($keystoreSha256 -ieq $policy.retiredKeystoreSha256 `
        -or $credentialSha256 -ieq $policy.retiredCredentialFileSha256) {
        throw "Refusing to register the retired pre-rotation E15 signing material."
    }

    $credentialAccess = Test-E15SigningCredentialAccess `
        -KeystorePath $resolvedKeystore `
        -CredentialPath $resolvedCredential `
        -KeyAlias $KeyAlias `
        -KeytoolPath $KeytoolPath `
        -ExpectedCertificateSha256 $policy.certificateSha256
    if (-not [bool]$credentialAccess.passed) {
        throw "Refusing to register unverified E15 signing credentials: $($credentialAccess.reasons -join ' ')"
    }

    $record = [pscustomobject]@{
        schemaVersion = 2
        state = "credential_rotated"
        passed = $true
        rotatedAtUtc = $RotatedAtUtc.ToUniversalTime().ToString("o")
        keyAlias = $KeyAlias
        certificateSha256 = $credentialAccess.certificateSha256
        keystoreSha256 = $keystoreSha256
        credentialFileSha256 = $credentialSha256
        credentialVerification = $policy.credentialVerification
        keytoolSha256 = $credentialAccess.keytoolSha256
    }
    $parent = [IO.Path]::GetDirectoryName($resolvedOutput)
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    $record | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $resolvedOutput -Encoding UTF8
    $validation = Test-E15SigningCredentialRotationRecord `
        -RecordPath $resolvedOutput `
        -KeystorePath $resolvedKeystore `
        -CredentialPath $resolvedCredential `
        -KeyAlias $KeyAlias `
        -RepositoryRoot $RepositoryRoot
    if (-not [bool]$validation.passed) {
        Remove-Item -LiteralPath $resolvedOutput -Force
        throw "Generated E15 signing rotation record failed validation: $($validation.reasons -join ' ')"
    }
    return $validation
}
