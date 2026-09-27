namespace Scanner.Desktop;
// Fixed city specialties approved for display. Informational only; never used in calculations.
internal static class CityBonuses
{
 internal static string For(string city)=>city switch
 {
  "Fort Sterling"=>"Wood +40% · Raw Chicken, Raw Mutton +10% · Hammer, Spear, Holy Staff, Plate Helmet, Cloth Robes +15%",
  "Lymhurst"=>"Fiber +40% · Raw Goose +10% · Sword, Bow, Arcane Staff, Leather Hood, Leather Shoes +15%",
  "Bridgewatch"=>"Stone +40% · Raw Goat +10% · Crossbow, Dagger, Cursed Staff, Plate Armor, Cloth Sandals +15%",
  "Martlock"=>"Hide +40% · Raw Beef +10% · Axe, Quarterstaff, Frost Staff, Plate Boots, Off-Hand +15%",
  "Thetford"=>"Ore +40% · Raw Pork +10% · Mace, Nature Staff, Fire Staff, Leather Jacket, Cloth Cowl +15%",
  "Caerleon"=>"Food, Gathering Gear, Gathering Tools, War Gloves, Shapeshifter Staff +15%",
  "Brecilien"=>"Cape, Bag, Potion +15%",
  _=>"No city selected."
 };
}
