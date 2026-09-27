param([string]$MetadataPath, [string]$NamesPath, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$data = Get-Content -LiteralPath $MetadataPath -Raw | ConvertFrom-Json
$names = @{}
Get-Content -LiteralPath $NamesPath -Raw | ConvertFrom-Json | ForEach-Object { $names[$_.UniqueName] = $_.LocalizedNames.'EN-US' }
$recipes = [System.Collections.Generic.List[object]]::new()
$excluded = [System.Collections.Generic.List[object]]::new()
function MarketId($id, $level) { if ([int]$level -gt 0 -and $id -notmatch '@') { return "$id@$level" }; return $id }
foreach ($kind in $data.items.PSObject.Properties) {
 foreach ($item in @($kind.Value)) {
  if (!$item.'@uniquename') { continue }
  $variants = @([pscustomobject]@{ Level=$item.'@enchantmentlevel'; Requirements=$item.craftingrequirements })
  foreach($ench in @($item.enchantments.enchantment)) { if($ench) { $variants += [pscustomobject]@{ Level=$ench.'@enchantmentlevel'; Requirements=$ench.craftingrequirements } } }
  foreach($variant in $variants) {
   $id = MarketId $item.'@uniquename' $variant.Level
   $route = 0
   foreach($req in @($variant.Requirements)) {
    if (!$req) { continue }
    $route++
    $resources = @($req.craftresource | Where-Object { $_ })
    $reason = $null
    if ($resources.Count -eq 0) { $reason = 'No material recipe (purchase/exchange or special acquisition)' }
    elseif ($resources.Count -gt 10) { $reason = 'More than ten materials' }
    elseif (!$names.ContainsKey($id)) { $reason = 'Output not in published market item index' }
    $materials = @()
    foreach($resource in $resources) {
     $mid = MarketId $resource.'@uniquename' $resource.'@enchantmentlevel'
     if (!$names.ContainsKey($mid) -or $mid -notmatch '^[A-Z][A-Z0-9_]{1,99}(?:@[1-4])?$') { $reason = "Material not in market item index: $mid" }
     $qty = [decimal]$resource.'@count'
     if ($qty -le 0) { $reason = 'Nonpositive or unspecified material quantity' }
     $mn = $names[$mid]; if (!$mn) { $mn=$mid }
     $materials += [pscustomobject]@{ItemId=$mid;Name=$mn;Quantity=$qty}
    }
    if ($reason) { $excluded.Add([pscustomobject]@{ItemId=$id;Route=$route;Reason=$reason}); continue }
    $category = $item.'@craftingcategory'; if (!$category) { $category=$item.'@shopsubcategory1' }; if(!$category){$category=$kind.Name}
    $category = (Get-Culture).TextInfo.ToTitleCase($category.Replace('_',' '))
    $tier = [int]$item.'@tier'; if ($tier -lt 1) { $tier = 1 }
    $amount = 1; if ($req.'@amountcrafted') { $amount=[int]$req.'@amountcrafted' }
    $name = $names[$id]; if (!$name) { $name=$id }
    if ([int]$variant.Level -gt 0) { $name += " [$tier.$($variant.Level)]" }
    if (@($variant.Requirements).Count -gt 1) { $name += " — recipe $route" }
    $recipes.Add([pscustomobject][ordered]@{ItemId=$id;Name=$name;Category=$category;Tier=$tier;OutputQuantity=$amount;Materials=$materials;Source='AODP item metadata · 2026-09-26';Variant=$route;SilverCost=[decimal]$req.'@silver'})
   }
  }
 }
}
$recipes | Sort-Object ItemId,Variant | ConvertTo-Json -Depth 12 | Set-Content -Encoding utf8 (Join-Path $OutputDirectory 'recipes.json')
$excluded | ConvertTo-Json -Depth 5 | Set-Content -Encoding utf8 (Join-Path $OutputDirectory 'catalog-exclusions.json')
$summary = [pscustomobject]@{Recipes=$recipes.Count;DistinctItems=@($recipes.ItemId | Sort-Object -Unique).Count;EnchantedRecipes=@($recipes | Where-Object ItemId -match '@').Count;Categories=@($recipes.Category | Sort-Object -Unique);Exclusions=@($excluded | Group-Object Reason | Select-Object Name,Count)}
$summary | ConvertTo-Json -Depth 6 | Set-Content -Encoding utf8 (Join-Path $OutputDirectory 'catalog-summary.json')
$summary | ConvertTo-Json -Depth 6

