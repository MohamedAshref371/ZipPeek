$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$repoPath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
[void][Reflection.Assembly]::LoadFrom((Join-Path $repoPath 'ZipPeek/bin/Debug/ZipPeek.exe'))
[System.Windows.Forms.Application]::EnableVisualStyles()
Add-Type -TypeDefinition @"
using System;
using System.Reflection;
public static class UiSmokeReflection {
    public static object Call(object target, string name, object[] arguments) {
        for (int i = 0; i < arguments.Length; i++)
            if (arguments[i] != null && arguments[i].GetType().FullName == "System.Management.Automation.PSObject")
                arguments[i] = arguments[i].GetType().GetProperty("BaseObject").GetValue(arguments[i], null);
        return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
    }
}
"@
$form = New-Object ZipPeek.Form1
try {
    $flags = [Reflection.BindingFlags]'Instance,NonPublic'
    $selector = $form.Controls['archiveTypeList']
    if ($selector.SelectedItem -ne 'ZIP') { throw 'ZIP must remain the default.' }
    [void]$form.Controls['treeZip'].Nodes.Add('old archive')
    $selector.SelectedIndex = 1
    if ($form.Controls['treeZip'].Nodes.Count -ne 0) { throw 'Format changes must clear the old tree.' }
    if ($form.Controls['statusLabel'].Text -ne 'Ready.') { throw '7z must be available.' }
    $entries = New-Object 'System.Collections.Generic.List[ZipPeek.ZipEntry]'
    foreach ($item in @(@('package/a.bin', 0, 1024, 2), @('package/nested/b.bin', 0, 1024, 2), @('package/nested/c.bin', 1, 2048, 1))) {
        $entry = New-Object ZipPeek.SevenZipArchiveEntry
        $entry.FileName = $item[0]
        $entry.UncompressedSize = 4096
        $entry.LastModified = Get-Date '2026-10-27 12:00'
        $entry.GetType().GetProperty('BlockId').SetValue($entry, [Nullable[long]]([long]$item[1]))
        $entry.GetType().GetProperty('BlockCompressedSize').SetValue($entry, [long]$item[2])
        $entry.GetType().GetProperty('BlockFileCount').SetValue($entry, [int]$item[3])
        $entries.Add($entry)
    }
    [ZipPeek.TreeViewHelper]::AddToTree($entries)
    $node = $form.Controls['treeZip'].Nodes[0]
    $selection = New-Object 'System.Collections.Generic.List[ZipPeek.SevenZipArchiveEntry]'
    [ZipPeek.FolderSetting]::SubfolderOption = 0
    [void][UiSmokeReflection]::Call($form, 'CollectSevenZipSelection', @($node, $selection))
    if ($selection.Count -ne 1) { throw 'Subfolder exclusion must apply before block expansion.' }
    $selection.Clear()
    [ZipPeek.FolderSetting]::SubfolderOption = 1
    [void][UiSmokeReflection]::Call($form, 'CollectSevenZipSelection', @($node, $selection))
    if ($selection.Count -ne 3) { throw 'Lazy folder selection must include nested files.' }
    $size = $form.GetType().GetMethod('CalculatingFolderSize', $flags).Invoke($form, @($node, $true, $null))
    if ($size.Item1 -ne 3 -or $size.Item2 -ne 3072) { throw 'Shared blocks must be counted once.' }
    $node.Expand()
    $selection.Clear()
    [void][UiSmokeReflection]::Call($form, 'CollectSevenZipSelection', @($node, $selection))
    if ($selection.Count -ne 3) { throw 'Expanded folder selection must include nested files.' }
    [void]$form.GetType().GetMethod('SetUiState', $flags).Invoke($form, @($false))
    if ($selector.Enabled -or $form.Controls['passwordTextBox'].Enabled) { throw 'Archive inputs must be disabled while busy.' }
    [void]$form.GetType().GetMethod('SetUiState', $flags).Invoke($form, @($true))
    $form.Controls['urlTextBox'].Text = 'https://example.com/archive.7z'
    $form.Controls['statusLabel'].Text = 'Loaded 3 files from 7z.'
    $form.StartPosition = 'Manual'
    $form.Location = New-Object System.Drawing.Point(-32000, -32000)
    $form.ShowInTaskbar = $false
    $form.Show()
    [System.Windows.Forms.Application]::DoEvents()
    $form.Controls['treeZip'].ExpandAll()
    $bitmap = New-Object System.Drawing.Bitmap($form.Width, $form.Height)
    try {
        $form.DrawToBitmap($bitmap, (New-Object System.Drawing.Rectangle(0, 0, $form.Width, $form.Height)))
        $bitmap.Save((Join-Path $PSScriptRoot 'obj/sevenzip-ui.png'))
    } finally { $bitmap.Dispose() }
    Write-Output 'PASS: 7z selector, lazy and expanded selections, folder settings, shared block totals, busy state, and rendering.'
} finally { $form.Dispose() }
