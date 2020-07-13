// This code does the following:
//	1. Connects to the folder in ADLS Gen2 Storage Account that is populated with the CDS data in CDM format from the Export to Data Lake serivce.
//	2. Accesses the model.json file.
//	3. Creates and saves the manifest.json file and the resolved definition file(s) for each entity in the storage account.The /binn directory contains
//	   two libraries that can be used as references.
// Note: The example-public-standards directory is already included in this project and contains sample entity definitions. It can also be found at 
// https://github.com/microsoft/CDM/tree/master/samples/example-public-standards

using Microsoft.CommonDataModel.ObjectModel.Cdm;
using Microsoft.CommonDataModel.ObjectModel.Storage;
using Microsoft.CommonDataModel.ObjectModel.Enums;
using System;
using System.Threading.Tasks;


namespace ConvertModelJsonToManifest {
    class Program {
        static async Task Main (string[] args) {

            // Path to the example-public-standards directory included in the project.  
            // string pathFromExeToExampleRoot = "../";

            // Instantiate a corpus.
            CdmCorpusDefinition corpus = new CdmCorpusDefinition();

            // Configuring the storage adapter pointing to the target local manifest location.
            Console.WriteLine("Configuring storage adapters.");
            // REPLACE the default information with your specific storage account information.
            var adlsAdapter = new ADLSAdapter("your-storage-account.dfs.core.windows.net", "/your-folder-name", "access key");

            // Access the specified storage account.
            adlsAdapter.Timeout = TimeSpan.FromMilliseconds(10000);
            corpus.Storage.Mount("adls", adlsAdapter);
            corpus.Storage.DefaultNamespace = "adls";
            var localRoot = corpus.Storage.FetchRootFolder("adls");

            // Use the example-public-standards directory to access a local copy of entity definition files.
            // corpus.Storage.Mount("cdm", new LocalAdapter(pathFromExeToExampleRoot + "example-public-standards"));            

            // Read the model.json file.
            CdmManifestDefinition manifest = await corpus.FetchObjectAsync<CdmManifestDefinition>("model.json");
            CdmManifestDefinition manifestAbstract = corpus.MakeObject<CdmManifestDefinition>(CdmObjectType.ManifestDef, "resolvedManifest");
            localRoot.Documents.Add(manifestAbstract);            
            manifestAbstract.Imports.Add("cdm:/foundations.cdm.json");

            // Create the unresolved and resolved entity definitions for each entity.
            foreach (CdmEntityDeclarationDefinition ent in manifest.Entities) {
                CdmEntityDefinition entdef = await corpus.FetchObjectAsync<CdmEntityDefinition>($"adls:/{ent.EntityName}.cdm.json/{ent.EntityName}");
                CdmEntityDefinition resEnt = await entdef.CreateResolvedEntityAsync(ent.EntityName, null, localRoot, ent.EntityName + "Res.cdm.json");
                manifestAbstract.Entities.Add(resEnt);
            }

            // Save the manifest.json file.
            await manifestAbstract.FileStatusCheckAsync();
            // Optionally, REPLACE the name of the manifest file.
            CdmManifestDefinition manRes = await manifestAbstract.CreateResolvedManifestAsync("test", "");
            await manRes.SaveAsAsync("test.manifest.cdm.json", true);
            Console.WriteLine("Manifest created.");
            Console.WriteLine("Entity defintion(s) created.");
        }
    }
}
