using System;
using System.Collections.Generic;

namespace Cutrium.Editor.Setup
{
    /// English copy paired with the Turkish Earth landmark source records.
    /// Landmark IDs are the stable join key so artwork paths and progression
    /// ordering remain independent from presentation language.
    public static class EarthLandmarkLocalizationContent
    {
        private static readonly IReadOnlyDictionary<string, EnglishEntry>
            EnglishById = new Dictionary<string, EnglishEntry>(
                StringComparer.Ordinal)
            {
                ["angkor-wat"] = Entry(
                    "Angkor Wat",
                    "Siem Reap / CAMBODIA",
                    "Founded in the 12th century as an immense Hindu temple, Angkor Wat later became a Buddhist sacred site. Its five-towered silhouette and more than a kilometre of reliefs make it one of the world's largest religious monuments. Its unusual west-facing plan and stone scenes of war, court life, and epic tales recount the power of the Khmer Empire in remarkable detail."),
                ["aspendos-tiyatrosu"] = Entry(
                    "Aspendos Ancient Theatre",
                    "Antalya / TURKEY",
                    "Built in the 2nd century AD, Aspendos is one of the best-preserved theatres surviving from the Roman world. Its monumental stage building and powerful acoustics are striking examples of Roman engineering. The seating carries sound to an audience of roughly 15,000, while its continued use for performances demonstrates the structure's extraordinary integrity."),
                ["aziz-vasil-katedrali"] = Entry(
                    "Saint Basil's Cathedral",
                    "Moscow / RUSSIA",
                    "Built on Red Square in the 16th century, the cathedral is known for onion domes with distinct colours and patterns. It is not a single hall, but nine interconnected chapels. Commissioned by Ivan the Terrible to commemorate the conquest of Kazan, its now-iconic vivid colours were developed during the centuries after its original construction."),
                ["big-ben-westminster"] = Entry(
                    "Big Ben and the Palace of Westminster",
                    "London / UNITED KINGDOM",
                    "Big Ben is actually the nickname of the Great Bell inside Elizabeth Tower, not the tower itself. The neighbouring Palace of Westminster is home to the Parliament of the United Kingdom. Rebuilt in the Gothic Revival style after the 1834 fire destroyed most of the old palace, its famous clock has marked London's rhythm since 1859."),
                ["borobudur-tapinagi"] = Entry(
                    "Borobudur Temple",
                    "Java / INDONESIA",
                    "Built in the 9th century, Borobudur is a monumental temple whose stepped terraces rise like a vast Buddhist mandala. Hundreds of Buddha statues and narrative reliefs take visitors on a symbolic journey toward enlightenment. Near the summit, 72 perforated stupas containing Buddha figures surround the central dome."),
                ["brandenburg-kapisi"] = Entry(
                    "Brandenburg Gate",
                    "Berlin / GERMANY",
                    "Built at the end of the 18th century, the Brandenburg Gate is one of Berlin's most powerful symbols. It stood at the boundary of division during the Cold War and came to represent German reunification after 1989. The Quadriga above the gate shows the goddess of victory driving a four-horse chariot toward the city."),
                ["burj-al-arab"] = Entry(
                    "Burj Al Arab",
                    "Dubai / UNITED ARAB EMIRATES",
                    "The sail-shaped Burj Al Arab rises from an artificial island off Dubai's coast. Its bold silhouette and lavish interiors have made it one of modern Dubai's most recognisable landmarks. An atrium nearly 180 metres high and a helipad projecting over the sea give the hotel's architecture an unexpected sense of scale."),
                ["burj-khalifa"] = Entry(
                    "Burj Khalifa",
                    "Dubai / UNITED ARAB EMIRATES",
                    "At 828 metres, Burj Khalifa is the tallest building in the world. Its three-lobed plan, inspired by a desert flower, helps the immense structure remain stable against the wind. With more than 160 floors combining a hotel, homes, offices, and observation decks, it functions almost like a vertical city."),
                ["buyuk-kanyon"] = Entry(
                    "Grand Canyon",
                    "Arizona / USA",
                    "Carved by the Colorado River over millions of years, the Grand Canyon displays Earth's geological past in colourful layers of rock. Some of its oldest exposed layers are nearly two billion years old. Stretching about 446 kilometres and dropping more than 1.6 kilometres in places, its scale is almost impossible to grasp at a glance."),
                ["chichen-itza"] = Entry(
                    "Chichen Itza",
                    "Yucatan / MEXICO",
                    "Chichen Itza was one of the most important cities of the Maya world. The steps and shadows of the Pyramid of Kukulcan create a striking serpent-like play of light, especially around the equinoxes. Its Great Ball Court and Sacred Cenote show how sport, belief, and observation of the sky met at the same centre."),
                ["kurtarici-isa-heykeli"] = Entry(
                    "Christ the Redeemer",
                    "Rio de Janeiro / BRAZIL",
                    "Standing on Mount Corcovado with open arms above the city, this Art Deco statue is a symbol of Rio de Janeiro. The 30-metre figure and its mountain-and-sea setting form one of the world's most recognisable silhouettes. Completed in 1931, its reinforced-concrete body is covered with millions of small soapstone tiles that softly reflect the light."),
                ["cn-kulesi"] = Entry(
                    "CN Tower",
                    "Toronto / CANADA",
                    "The 553.3-metre CN Tower was once the world's tallest free-standing structure. Glass-floor observation areas and the outdoor EdgeWalk add a dramatic sense of height to the Toronto view. Opened in 1976, the tower was originally designed to carry communications signals above the city's rapidly rising skyscrapers."),
                ["cin-seddi"] = Entry(
                    "Great Wall of China",
                    "CHINA",
                    "The Great Wall is not one continuous wall, but a vast defensive network built by different dynasties over many centuries. Its walls, passes, and watchtowers form a system spanning thousands of kilometres. Depending on the region, builders used rammed earth, stone, or brick, while smoke, fire, and flag signals carried messages rapidly between towers."),
                ["dubrovnik-surlari"] = Entry(
                    "Walls of Dubrovnik",
                    "Dubrovnik / CROATIA",
                    "Dubrovnik's stone walls run for almost two kilometres and completely surround the historic city on the Adriatic coast. Views of orange rooftops and blue sea from the towers create the city's most memorable panorama. Strengthened continuously between the 13th and 17th centuries, these defences helped the prosperous Republic of Ragusa preserve its independence for hundreds of years."),
                ["efes-antik-kenti"] = Entry(
                    "Ancient City of Ephesus",
                    "Izmir / TURKEY",
                    "Ephesus was one of the largest port and trading cities of the Roman era. The Library of Celsus, marble streets, and Great Theatre still convey its splendour. The nearby Temple of Artemis was counted among the Seven Wonders of the Ancient World, while mosaics and frescoes in the terrace houses offer a close view of wealthy urban life."),
                ["el-hamra-sarayi"] = Entry(
                    "Alhambra Palace",
                    "Granada / SPAIN",
                    "The Alhambra is the palace and fortress complex of the Nasrid rulers. Fine geometric decoration, inscriptions, courtyards, and flowing water create a calm yet captivating atmosphere. Its Arabic name means Red Castle, a meaning made clear when the outer walls take on a warm colour at sunset."),
                ["eyfel-kulesi"] = Entry(
                    "Eiffel Tower",
                    "Paris / FRANCE",
                    "Built for the 1889 World's Fair, the Eiffel Tower was initially designed as a temporary structure. Its iron lattice was once criticised, but is now Paris's most powerful symbol. The roughly 300-metre tower proved its practical value through radio experiments and communications antennas, saving it from the planned dismantling."),
                ["fuji-dagi"] = Entry(
                    "Mount Fuji",
                    "JAPAN",
                    "At 3,776 metres, Mount Fuji is Japan's highest mountain and a stratovolcano still considered active. Its nearly perfect cone has inspired artists, pilgrims, and travellers for centuries. The sacred mountain last erupted in 1707 and became an enduring symbol in art beyond Japan through Hokusai's celebrated prints."),
                ["fushimi-inari-taisha"] = Entry(
                    "Fushimi Inari Taisha",
                    "Kyoto / JAPAN",
                    "Dedicated to Inari, associated with prosperity and rice, this Shinto shrine is famous for thousands of bright red torii gates climbing the mountain. The gates form a seemingly endless tunnel through the forest. Each gate represents a donation by a person or business, while fox statues along the path are regarded as Inari's messengers."),
                ["galata-kulesi"] = Entry(
                    "Galata Tower",
                    "Istanbul / TURKEY",
                    "The roots of Galata Tower's present form reach back to the 14th-century Genoese settlement, and the tower rises above the Golden Horn. Its summit offers a wide panorama of the Bosphorus and the historic peninsula. Used over the centuries as a defensive tower, prison, and fire lookout, it was repaired many times after earthquakes and fires."),
                ["golden-gate-koprusu"] = Entry(
                    "Golden Gate Bridge",
                    "San Francisco / USA",
                    "Opened in 1937, the Golden Gate is an immense suspension bridge crossing a famously foggy strait. Its International Orange colour makes it visible in the mist while harmonising with the surrounding hills. Its 1,280-metre main span was a world record when completed, and its Art Deco towers rise about 227 metres above the water."),
                ["gobeklitepe"] = Entry(
                    "Gobekli Tepe",
                    "Sanliurfa / TURKEY",
                    "Gobekli Tepe's T-shaped stone pillars, carved with animals, date back to around 9600 BC. Older than pottery and settled farming communities, this monumental site transformed how we understand human history. The lack of clear domestic traces in its circular structures, whose pillars can exceed five metres, suggests that the site may have hosted large gatherings and rituals."),
                ["kapalicarsi"] = Entry(
                    "Grand Bazaar",
                    "Istanbul / TURKEY",
                    "With roots in the 15th century, the Grand Bazaar is a living commercial maze of vaulted passages and thousands of shops. Crafts ranging from jewellery to carpets carry Istanbul's centuries-old trading culture. More than 60 streets woven through inns and covered markets still reflect the historic city pattern in which different trades gathered in distinct areas."),
                ["ayasofya"] = Entry(
                    "Hagia Sophia",
                    "Istanbul / TURKEY",
                    "Completed in 537, Hagia Sophia's immense dome was a turning point in architectural history. Used over the centuries as a church, mosque, and museum, the building brings Byzantine and Ottoman heritage together in one space. The pendentives connecting the dome to the square main hall were a major engineering innovation, while mosaics and monumental calligraphic roundels make its different eras visible side by side."),
            };

        public static EnglishEntry GetEnglish(string landmarkId)
        {
            if (landmarkId == null
                || !EnglishById.TryGetValue(
                    landmarkId,
                    out EnglishEntry entry))
            {
                throw new InvalidOperationException(
                    $"Missing English localization for landmark " +
                    $"'{landmarkId ?? "<null>"}'.");
            }

            return entry;
        }

        private static EnglishEntry Entry(
            string title,
            string sector,
            string description) =>
            new EnglishEntry(title, sector, description);

        public readonly struct EnglishEntry
        {
            public EnglishEntry(
                string title,
                string sector,
                string description)
            {
                Title = title;
                Sector = sector;
                Description = description;
            }

            public string Title { get; }
            public string Sector { get; }
            public string Description { get; }
        }
    }
}
