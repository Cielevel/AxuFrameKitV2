$dir = "d:\UnityProject\AxU\AxuFrameKitV2\Assets\AxuIndieModules\StatPipeline"
$files = Get-ChildItem -Path $dir -Filter "*.cs" -Recurse
foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $hasOldNamespace = $content -match "namespace AxuIndieModules\.StatPipeline\.Script"
    
    # Update namespace
    $content = $content -replace "namespace AxuIndieModules\.StatPipeline\.Script(\.[\w\.]+)?", "namespace AxuIndieModules.StatPipeline"
    
    if ($hasOldNamespace) {
        # If it's a module file, remove all using statements to the module itself (flattened)
        $content = $content -replace "using AxuIndieModules\.StatPipeline\.Script(\.[\w\.]+)?;\r?\n?", ""
    } else {
        # If it's an external file, update using to the new base namespace
        $content = $content -replace "using AxuIndieModules\.StatPipeline\.Script(\.[\w\.]+)?", "using AxuIndieModules.StatPipeline"
    }
    
    [System.IO.File]::WriteAllText($file.FullName, $content)
    Write-Host "Processed $($file.FullName)"
}
