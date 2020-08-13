<p align="center"><img src="https://i.imgur.com/MkVWxYv.png"/></p>

# ArkIntel
World save parser for ARK: Survival Evolved servers, written specifically for my personal server running Extinction Core.

# Dependencies
1. WinSCP - Downloads the world save file
2. [Ark-Tools](https://github.com/Qowyn/ark-tools) - Converts the world save file into .json format

# Usage
1. Obtain a world save file. This can be done manually; place a world save file in the working directory to use it. Alternatively, using the "Refresh Data" button in the UI will grab data from an assigned FTP server. The current file it looks for is "TheVolcano.ark" reflecting the server this was built for.

2. Run the tool; it will handle the backend execution for Ark-Tools, which will output .json data for all wild dinos to ./output dir.

3. Use the dino list on the left side to view stats for specific dinos.

4. Click "Show Map" to view a map with approximate locations of each instance of a selected dino. Click on listings in the table to show their location on the map.

# Rrandom info
Cells containing baselevel info in the table are color-coded. These colors are also reflected on the map view.

* \>90 - green
* \>60 - yellow
* \>30 - orange
* \<30 - red

There's some logic in place to somewhat clean up and label certain instances of dinos; for example, Scorched Earth dinos are noted with "(SE)" after their name. However, this labeling is somewhat buggy and hacky; specifically Rock Golems & leeches will occasionally appear multiple times due to the different naming conventions their classes use.

* (SE) - Scorched Earth
* (Child) - Adolescent dinos
* (Base) - Non-Extinction Core dinos (running this tool without ExCo will result in most of the list having this identifier)
* (L) (F) (P) - Lightning/Fire/Poison respectively.
