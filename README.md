# nps-browser
A cross-platform port of NPS Browser to Avalonia done by PJB3005 from here:
https://gitlab.com/PJB3005/nps-browser

While being cross-platform wasn't my goal, it's the only source code still available. 
Avalonia (the cross-platform WPF-like Framework) feels less stable than WPF, and with larger file sizes,
however with the source code I have been able to make some improvements to NPS Browser, which no longer
seems to be maintained - especially for bulk file collection.

Among these improvements:
- Can download multiple selections with one click
- Added a Download All w/ Patches option that bypasses patch prompts
- Added an OR operator into the search "||"
- PS3 games now unpack into a PS3 folder, and then by game, since previous methods would prevent games from downloading if they shared a title, also disassociated game PKG files from their DLC files
