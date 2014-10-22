AdventureTerre
==============

This repo contains an Azure version of the Adventure Orleans sample with a few modifications.  

In order to publish this sample to Azure, you need to create a Cloud Service in the region you want 
as well as a Storage Account (preferebly in the same region.  Take the Storage Account's name and primary key and put them
into the following files:
* AdventureTerreSilos/OrleansConfiguration.xml
* AdventureTerreAzure/ServiceConfiguration.Cloud.cscfg


You'll also need to install the Orleans framework which you can download from here [https://orleans.codeplex.com/](https://orleans.codeplex.com/).

