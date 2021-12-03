using System;
using System.Collections.Generic;
using System.Linq;
using PV178.Homeworks.HW04.DataLoading.DataContext;
using PV178.Homeworks.HW04.DataLoading.Factory;
using PV178.Homeworks.HW04.Model;
using PV178.Homeworks.HW04.Model.Enums;

namespace PV178.Homeworks.HW04
{
    public class Queries
    {
        private IDataContext? _dataContext;
        public IDataContext DataContext => _dataContext ??= new DataContextFactory().CreateDataContext();

        /// <summary>
        /// SFTW si vyžiadala počet útokov, 
        /// ktoré sa udiali v krajinách začinajúcich na písmeno 'A', a kde obete boli muži starší ako 25 rokov.
        /// </summary>
        /// <returns>The query result</returns>
        public int AttacksACountriesMaleOlderThanTwentyfiveQuery()
        {
            return DataContext.SharkAttacks
                .Join(DataContext.AttackedPeople, attack => attack.AttackedPersonId, person => person.Id, (attack, person) => (attack, person))
                .Where(pair => pair.person.Age > 25 && pair.person.Sex == Sex.Male).Select(pair => pair.attack)
                .Join(DataContext.Countries, attack => attack.CountryId, country => country.Id, (attack, country) => (attack, country))
                .Where(pair => pair.country.Name != null && pair.country.Name.StartsWith('A')).Count();
        }

        /// <summary>
        /// Prišla nám ďalšia požiadavka od našej milovanej SFTW. 
        /// Chcú od nás 5 názvov krajín s najviac útokmi, kde žraloky vážili viac ako 300kg.
        /// Požadujú, aby tieto data boli zoradené abecedne.
        /// </summary>
        /// <returns>The query result</returns>
        public List<string> FiveCountriesWithTopNumberOfAttackSharksHeavierThanThreeHundredQuery()
        {
            return DataContext.Countries
                .GroupJoin(DataContext.SharkAttacks
                    .Join(DataContext.SharkSpecies, attack => attack.SharkSpeciesId, shark => shark.Id, (attack, shark) => (attack, shark.Weight))
                    .Where(pair => pair.Weight > 300)
                    .Select(pair => pair.attack), 
                    country => country.Id, attack => attack.CountryId, (country, attacks) => (country, attacks.Count()))
                .Where(pair => pair.country.Name != null)
                .OrderByDescending(pair => pair.Item2)
                .Take(5)
                .OrderBy(pair => pair.country.Name)
                .Select(pair => pair.country.Name ?? "")
                .ToList();
        }

        /// <summary>
        /// SFTW chce zistiť, ktoré žraloky útočia najviac na mužov. 
        /// Chcú od nás top 5 id žralokov, zoradených zostupne podľa počtu útokov na mužoch.
        /// </summary>
        /// <returns>The query result</returns>
        public List<int> FiveSharksOrderedByNumberOfAttacksOnMenQuery()
        {
            return DataContext.SharkAttacks
                .GroupJoin(DataContext.AttackedPeople
                    .Where(person => person.Sex == Sex.Male),
                    attack => attack.AttackedPersonId, person => person.Id, (attack, people) => (attack, people.Count()))
                .GroupBy(pair => pair.attack.SharkSpeciesId)
                .Select(grping => grping.Aggregate((pair1, pair2) => (pair1.attack, pair1.Item2 + pair2.Item2)))
                .OrderByDescending(pair => pair.Item2)
                .Take(5)
                .Select(pair => pair.attack.SharkSpeciesId)
                .ToList();
        }

        /// <summary>
        /// Napísať hocijaký LINQ dotaz musí byť pre Vás už triviálne. 
        /// SFTW chce zistiť, či kazdý žralok, ktorý je tažší ako 200 kg zaútočil na obe pohlavia. 
        /// Reprezentujte výsledok ako boolean.
        /// </summary>
        /// <returns>The query result</returns>
        public bool GenderPerceptionBySharskHeavierThanTwoHundredQuery()
        {
            return DataContext
                .SharkSpecies.Where(shark => shark.Weight > 200)
                .GroupJoin(DataContext.SharkAttacks
                    .Join(DataContext.AttackedPeople, attack => attack.AttackedPersonId, person => person.Id,
                        (attack, person) => (attack, person)),
                        shark => shark.Id, pair => pair.attack.SharkSpeciesId,
                        (shark, pairs) => pairs.Select(p => p.person.Sex).Distinct())
                .All(sexes => sexes.Contains(Sex.Female) && sexes.Contains(Sex.Male));
        }

        /// <summary>
        /// SFTW potrebuje súrne informácie pre International Dictatorship Organization (IDO). 
        /// Chcú vedieť aký je počet útokov, ktoré sa udiali v 'Territory' alebo v 'Monarchy' a ktoré neboli preukázané ako fatálne.
        /// Do vyhľadavánia chceme zahrnúť, len tie krajiny, kde je birthrate väčší ako 10.
        /// </summary>
        /// <returns>The query result</returns>
        public int NonFatalAttacksInTerrOrMonWithBirthrateHigherThanTenQuery()
        {
            return DataContext.SharkAttacks
                .Where(attack => attack.AttackSeverenity != AttackSeverenity.Fatal)
                .Join(DataContext.Countries
                    .Where(country =>
                        country.Birthrate > 10 &&
                        (country.GovernmentForm == GovernmentForm.Territory || country.GovernmentForm == GovernmentForm.Monarchy)),
                    attack => attack.CountryId, country => country.Id, (attack, country) => (attack, country))
                .Count();
        }

        /// <summary>
        /// Každý túži po prezývke a žralok nie je výnimkou. Keď na Vás pekne volajú, hneď Vám lepšie chutí. 
        /// Potrebujeme získať všetkých žralokov, ktoré nemajú prezývku(AlsoKnownAs) a k týmto žralokom krajinu v ktorej najviac útočili.
        /// Samozrejme to SFTW chce v podobe Dictionary, kde key bude názov žraloka a value názov krajiny.
        /// Len si predstavte tie rôznorodé prezývky, napr. Devil of Kyrgyzstan.
        /// </summary>
        /// <returns>The query result</returns>
        public Dictionary<string, string> WithoutNickNameCountryWithMostAttacksQuery()
        {
            return DataContext.SharkSpecies
                .Where(shark => shark.AlsoKnownAs is null || shark.AlsoKnownAs == "")
                .GroupJoin(DataContext.SharkAttacks
                        .Join(DataContext.Countries, attack => attack.CountryId, country => country.Id, (attack, country) => (attack, country)),
                    shark => shark.Id, pair => pair.attack.SharkSpeciesId, (shark, pairs) => (shark, pairs.Select(p => p.country.Name)))
                .Select(pair => (
                    pair.shark,
                    pair.Item2
                        .Where(name => name != null)
                        .GroupBy(n => n)
                        .OrderByDescending(g => g.Count())
                        .Select(g => g.Key ?? "")
                        .First()))
                .ToDictionary(pair => pair.shark.Name ?? "", pair => pair.Item2);
        }

        /// <summary>
        /// Ohúrili ste SFTW natoľko, že si u Vás objednali rovno textové výpisy. Samozrejme, že sa to dá zvladnúť pomocou LINQ. 
        /// Chcú aby ste pre všetky fatálne útoky v štátoch na písmenko 'A' a 'B', urobili výpis v podobe: 
        /// "{Meno obete} was attacked in {názov štátu} by {latinský názov žraloka}"
        /// Získané pole zoraďte abecedne a vraťte prvých 5 viet.
        /// </summary>
        /// <returns>The query result</returns>
        public List<string> InformationsAboutCountriesOnAorBAndFatalAttacksQuery()
        {
            return DataContext.SharkAttacks
                .Where(attack => attack.AttackSeverenity == AttackSeverenity.Fatal)
                .Join(DataContext.Countries, attack => attack.CountryId, country => country.Id, (attack, country) => (attack, country))
                .Where(pair => pair.country.Name != null && (pair.country.Name.StartsWith("A") || pair.country.Name.StartsWith("B")))
                .Join(DataContext.AttackedPeople, pair => pair.attack.AttackedPersonId, person => person.Id, (pair, person) => (pair.attack, pair.country, person))
                .Join(DataContext.SharkSpecies, tripple => tripple.attack.SharkSpeciesId, shark => shark.Id, (tripple, shark) => (tripple.attack, tripple.country, tripple.person, shark))
                .Select(quad => $"{quad.person.Name} was attacked in {quad.country.Name} by {quad.shark.LatinName}")
                .OrderBy(s => s)
                .Take(5)
                .ToList();
        }

        /// <summary>
        /// SFTW pretlačil nový zákon. Chce pokutovať štáty v Európe, ktorých názvy začinajú na tieto písmená <A, B, C,... L>.
        /// Každý z týchto štátov dostane pokutu za každý útok na ich území a to buď 250 meny danej krajiny alebo 300 meny danej krajiny (ak bol fatálny).
        /// Ak útok nebol preukázany ako fatal alebo non-fatal, štát za takýto útok nie je pokutovaný.
        /// Vety budú zoradené zostupne podľa výšky pokuty.
        /// Opäť od Vás požadujú neštandardné formátovanie: "{Názov krajiny}: {Pokuta} {Mena danej krajiny}"
        /// Czech Republic: 550 CZK
        /// Hungary: 0 HUF
        /// Narnia: 600 G
        /// </summary>
        /// <returns>The query result</returns>
        public List<string> InfoAboutFinesInEuropeQuery()
        {
            return DataContext.Countries
                .Where(country => country.Continent == "Europe")
                .Where(country => country.Name != null && (country.Name[0] >= 'A' && country.Name[0] <= 'L'))
                .GroupJoin(DataContext.SharkAttacks,
                    country => country.Id,
                    attack => attack.CountryId,
                    (country, attacks) => (country, attacks.Aggregate(0, (acc, attack) =>
                    {
                        if (attack.AttackSeverenity == AttackSeverenity.Fatal) acc += 300;
                        else if (attack.AttackSeverenity == AttackSeverenity.NonFatal) acc += 250;
                        return acc;
                    })))
                .OrderByDescending(pair => pair.Item2)
                .ThenBy(pair => pair.country.Name)
                .Select(pair => $"{pair.country.Name}: {pair.Item2} {pair.country.CurrencyCode}")
                .ToList();
        }

        /// <summary>
        /// CEO chce kandidovať na prezidenta celej planéty. Chce zistiť ako ma štylizovať svoju rétoriku aby zaujal čo najviac krajín.
        /// Preto od Vás chce, aby ste mu pomohli zistiť aké percentuálne zastúpenie majú jednotlivé typy vlád.
        /// Požaduje to ako jeden string: "{typ vlády}: {percentuálne zastúpenie}%, ...". 
        /// Výstup je potrebné mať zoradený, od najväčších percent po najmenšie a percentá sa budú zaokrúhľovať na jedno desatinné číslo.
        /// Pre zlúčenie použite Aggregate(..).
        /// </summary>
        /// <returns>The query result</returns>
        public string StatisticsAboutGovernemntsQuery()
        {
            return DataContext.Countries
                .GroupBy(country => country.GovernmentForm)
                .Select(g => (g.Key, g.Count()))
                .OrderByDescending(i => i.Item2)
                .Select(pair => (pair.Key, (double)pair.Item2 / DataContext.Countries.Count() * 100))
                .Aggregate("", (r, pair) => r + $", {pair.Key}: {Math.Round(pair.Item2, 1):0.0}%")
                .Substring(2);
        }

        /// <summary>
        /// Oslovili nás surfisti. Chcú vedieť, či sú ako skupina viacej ohrození žralokmi. 
        /// Súrne potrebujeme vedieť koľko bolo fatálnych útokov na surfistov("surf", "Surf", "SURF") 
        /// a aký bol ich premierný vek(zaokrúliť na 2 desatinné miesta). 
        /// Zadávateľ úlohy nám to, ale skomplikoval. Tieto údaje chce pre každý kontinent.
        /// </summary>
        /// <returns>The query result</returns>
        public Dictionary<string, Tuple<int, double>> InfoForSurfersByContinentQuery()
        {
            return DataContext.Countries
                .Join(
                    DataContext.SharkAttacks.Where(attack => attack.Activity != null && attack.Activity.ToLower().Contains("surf") && attack.AttackSeverenity == AttackSeverenity.Fatal),
                    country => country.Id, attack => attack.CountryId,
                    (country, attack) => (country, attack))
                .Join(DataContext.AttackedPeople, pair => pair.attack.AttackedPersonId, person => person.Id, (pair, person) => (pair.country, pair.attack, person))
                .GroupBy(tripple => tripple.country.Continent)
                .Select(tripples => (tripples.Key, tripples.Count(), tripples.Where(tripple => tripple.person.Age != null).Average(tripple => tripple.person.Age)))
                .ToDictionary(tripple => tripple.Key ?? "", tripple => new Tuple<int, double>(tripple.Item2, Math.Round(tripple.Item3 ?? 0, 2)));
        }

        /// <summary>
        /// Zaujíma nás 10 najťažších žralokov na planéte a krajiny Severnej Ameriky. 
        /// CEO požaduje zoznam dvojíc, kde pre každý štát z danej množiny bude uvedený zoznam žralokov z danej množiny, ktorí v tom štáte útočili.
        /// Pokiaľ v nejakom štáte neútočil žiaden z najťažších žralokov, zoznam žralokov bude prázdny.
        /// SFTW požaduje prvých 5 položiek zoznamu dvojíc, zoradeného abecedne podľa mien štátov.

        /// </summary>
        /// <returns>The query result</returns>
        public List<Tuple<string, List<SharkSpecies>>> HeaviestSharksInNorthAmericaQuery()
        {
            return DataContext.Countries
                .Where(country => country.Continent == "North America")
                .GroupJoin(DataContext.SharkAttacks
                    .Join(DataContext.SharkSpecies
                        .OrderByDescending(shark => shark.Weight)
                        .Take(10), attack => attack.SharkSpeciesId,
                        shark => shark.Id,
                        (attack, shark) => (attack, shark)),
                    country => country.Id,
                    pair => pair.attack.CountryId,
                    (country, pairs) => (country, pairs.Select(pair => pair.shark)))
                .Select(pair => new Tuple<string, List<SharkSpecies>>(pair.country.Name ?? "", pair.Item2.Distinct().ToList()))
                .OrderBy(pair => pair.Item1)
                .Take(5)
                .ToList();
        }

        /// <summary>
        /// Zistite nám prosím všetky nefatálne útoky, ktoré mal na vine žralok s prezývkou "White death". 
        /// Zaujímajú nás útoky z obdobia medzi 3.3.1960 - 12.11.1980 (vrátane) a ľudia, ktorých meno začína na písmeno z intervalu <U, Z>.
        /// Výstup požadujeme ako zoznam mien zoradených abecedne.
        /// </summary>
        /// <returns>The query result</returns>
        public List<string> FailedAttemptOfWhiteDeathOnPeopleBetweenUAndZQuery()
        {
            return DataContext.SharkAttacks
                .Where(attack => attack.AttackSeverenity == AttackSeverenity.NonFatal)
                .Where(attack => attack.DateTime >= new DateTime(1960, 3, 3) && attack.DateTime <= new DateTime(1980, 11, 11))
                .Join(DataContext.SharkSpecies
                    .Where(shark => shark.AlsoKnownAs == "White death"),
                    attack => attack.SharkSpeciesId, shark => shark.Id, (attack, shark) => (attack, shark))
                .Join(DataContext.AttackedPeople
                    .Where(person => person.Name != null && person.Name[0] >= 'U' && person.Name[0] <= 'Z'),
                    pair => pair.attack.AttackedPersonId, person => person.Id, (pair, person) => person.Name ?? "")
                .OrderBy(s => s)
                .ToList();
        }

        /// <summary>
        /// Myslíme si, že rýchlejší žralok ma plnší žalúdok. 
        /// Požadujeme údaj o tom koľko percent útokov má na svedomí najrýchlejší a najpomalší žralok.
        /// Výstup požadujeme vo formáte: "{percentuálne zastúpenie najrýchlejšieho}% vs {percentuálne zastúpenie najpomalšieho}%"
        /// Perc. zastúpenie zaokrúhlite na jedno desatinné miesto.
        /// </summary>
        /// <returns>The query result</returns>
        public string FastestVsSlowestQuery()
        {
            var fastestAttacks = DataContext.SharkAttacks
                .Count(attack => attack.SharkSpeciesId == DataContext.SharkSpecies
                    .OrderByDescending(shark => shark.TopSpeed)
                    .First().Id) / (double)DataContext.SharkAttacks.Count() * 100;
            var slowestAttacks = DataContext.SharkAttacks
                .Count(attack => attack.SharkSpeciesId == DataContext.SharkSpecies
                    .OrderBy(shark => shark.TopSpeed)
                    .First().Id) / (double)DataContext.SharkAttacks.Count() * 100;
            return $"{Math.Round(fastestAttacks, 1):0.0}% vs {Math.Round(slowestAttacks, 1):0.0}%";
        }

        /// <summary>
        /// Prišla nám požiadavka z hora, aby sme im vrátili zoznam, 
        /// v ktorom je textová informácia o KAŽDOM človeku na ktorého zaútočil žralok v štáte Bahamas.
        /// Táto informácia je taktiež v tvare:
        /// {meno človeka} was attacked by {latinský názov žraloka}
        /// 
        /// Ale pozor váš nový nadriadený ma panický strach z operácie join alebo group join. 
        /// Musíte si poradiť ináč. Skúste sa zamyslieť, či by vám pomohla metóda Zip.
        /// Zistite tieto informácie bez spojenia hocijakých dvoch tabuliek (môžete ale použiť metódu Zip)
        /// </summary>
        /// <returns>The query result</returns>
        public List<string> ListOfAttacksInBahamasWithoutJoinQuery()
        {
            return DataContext.SharkAttacks
                .Where(attack => attack.CountryId == DataContext.Countries.First(country => country.Name == "Bahamas").Id)
                .Select(attack => (attack, DataContext.AttackedPeople.First(person => person.Id == attack.AttackedPersonId)))
                .Select(pair => (pair.attack, pair.Item2, DataContext.SharkSpecies.First(shark => shark.Id == pair.attack.SharkSpeciesId)))
                .Select(tripple => $"{tripple.Item2.Name} was attacked by {tripple.Item3.LatinName}")
                .ToList();
        }

        /// <summary>
        /// Na koniec vašej kariéry Vám chceme všetci poďakovať a pripomenúť Vám vašu mlčanlivosť. 
        /// Ako výstup požadujeme počet krajín v ktorých žralok nespôsobil smrť. Aj tie krajiny, kde žralok vôbec neútočil. 
        /// V tomto prípade za smrť považujeme iba ak je útok fatal.
        /// </summary>
        /// <returns>The query result</returns>
        public int SafeCountriesQuery()
        {
            return DataContext.Countries
                .GroupJoin(DataContext.SharkAttacks, country => country.Id, attack => attack.CountryId,
                (country, attacks) => (country, attacks.Select(attack => attack.AttackSeverenity)))
                .Where(pair => !pair.Item2.Contains(AttackSeverenity.Fatal))
                .Count();
        }
    }
}
