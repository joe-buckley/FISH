# Fish

This is a mod for **The Long Dark** by Hinterland Games, Inc whih overhauls the fishing system.

Overview:

Lakes now have fish stocks simulated using the Verhulst population model. 

The more a fish stock is depleted, the harder it is to catch fish, and the longer the fish stock will take longer to rebound. 


Cabins on the coast have transient fish stocks, or shoals, which pass under them occassionally.

No shoal, no fish, shoal - lots of fish.


Fish weights have been adjusted using a long-tailed poisson distribution model. 

(no logic, but it works, and hey, Poisson!)


In-game fish mesh-models are rescaled by fish-length and fish type.


Catch probability is no longer modelled using next fish caught at times.



Settings: 


FishConfig.json stores the FISH settings and is read at the start of a new game,

After which the parameters are saved in the games saved game file.


At any time in game, settings can be reset from the config file using the 

developer console command FISH-reset-from-config-file.


FishConfig.json contains further details about the game settings.





