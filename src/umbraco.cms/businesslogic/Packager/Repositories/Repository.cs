using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Net;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using umbraco.IO;

namespace umbraco.cms.businesslogic.packager.repositories
{
    public class Repository
    {        
        public string Guid { get; private set; }

        public string Name { get; private set; }

        public string RepositoryUrl { get; private set; }

        public string WebserviceUrl { get; private set; }


        public RepositoryWebservice Webservice
        {
            get
            {

                if (!WebserviceUrl.Contains("://"))
                {
                    WebserviceUrl = RepositoryUrl.Trim('/') + "/" + WebserviceUrl.Trim('/');
                }

                var repo = new RepositoryWebservice(WebserviceUrl);
                return repo;
            }
        }

        public SubmitStatus SubmitPackage(string authorGuid, PackageInstance package, byte[] doc)
        {

            string packageName = package.Name;
            string packageGuid = package.PackageGuid;
            string description = package.Readme;
            string packageFile = package.PackagePath;


            System.IO.FileStream fs1 = null;

            try
            {

                byte[] pack = new byte[0];
                fs1 = System.IO.File.Open(IOHelper.MapPath(packageFile), FileMode.Open, FileAccess.Read);
                pack = new byte[fs1.Length];
                fs1.Read(pack, 0, (int) fs1.Length);
                fs1.Close();
                fs1 = null;

                byte[] thumb = new byte[0]; //todo upload thumbnail... 

                return Webservice.SubmitPackage(Guid, authorGuid, packageGuid, pack, doc, thumb, packageName, "", "", description);
            }
            catch (Exception ex)
            {
                LogHelper.Error<Repository>("An error occurred in SubmitPackage", ex);

                return SubmitStatus.Error;
            }
        }

        public static List<Repository> getAll()
        {

            var repositories = new List<Repository>();

            foreach (var r in UmbracoConfiguration.Current.UmbracoSettings.PackageRepositories.Repositories)
            {
                var repository = new Repository
                {
                    Guid = r.Id.ToString(),
                    Name = r.Name
                };

                repository.RepositoryUrl = r.RepositoryUrl;
                repository.WebserviceUrl = repository.RepositoryUrl.Trim('/') + "/" + repository.WebserviceUrl.Trim('/');
                if (r.HasCustomWebServiceUrl)
                {
                    string wsUrl = r.WebServiceUrl;

                    if (wsUrl.Contains("://"))
                    {
                        repository.WebserviceUrl = r.WebServiceUrl;
                    }
                    else
                    {
                        repository.WebserviceUrl = repository.RepositoryUrl.Trim('/') + "/" + wsUrl.Trim('/');
                    }
                }

                repositories.Add(repository);
            }

            return repositories;
        }

        public static Repository getByGuid(string repositoryGuid)
        {
            Guid id;
            if (System.Guid.TryParse(repositoryGuid, out id) == false)
            {
                throw new FormatException("The repositoryGuid is not a valid GUID");
            }

            var found = UmbracoConfiguration.Current.UmbracoSettings.PackageRepositories.Repositories.FirstOrDefault(x => x.Id == id);
            if (found == null)
            {
                return null;
            }
            
            var repository = new Repository
            {
                Guid = found.Id.ToString(),
                Name = found.Name
            };

            repository.RepositoryUrl = found.RepositoryUrl;
            repository.WebserviceUrl = repository.RepositoryUrl.Trim('/') + "/" + repository.WebserviceUrl.Trim('/');

            if (found.HasCustomWebServiceUrl)
            {
                string wsUrl = found.WebServiceUrl;

                if (wsUrl.Contains("://"))
                {
                    repository.WebserviceUrl = found.WebServiceUrl;
                }
                else
                {
                    repository.WebserviceUrl = repository.RepositoryUrl.Trim('/') + "/" + wsUrl.Trim('/');
                }
            }

            return repository;
        }

        //shortcut method to download pack from repo and place it on the server...
        public string fetch(string packageGuid)
        {

            return fetch(packageGuid, string.Empty);

        }

        public bool HasConnection()
        {

            string strServer = this.RepositoryUrl;

            try
            {

                HttpWebRequest reqFP = (HttpWebRequest) HttpWebRequest.Create(strServer);
                HttpWebResponse rspFP = (HttpWebResponse) reqFP.GetResponse();

                if (HttpStatusCode.OK == rspFP.StatusCode)
                {

                    // HTTP = 200 - Internet connection available, server online
                    rspFP.Close();

                    return true;

                }
                else
                {

                    // Other status - Server or connection not available

                    rspFP.Close();

                    return false;

                }

            }
            catch (WebException)
            {

                // Exception - connection not available

                return false;

            }
        }

        public string fetch(string packageGuid, string key)
        {

            byte[] fileByteArray = new byte[0];

            if (key == string.Empty)
            {
                if (UmbracoConfiguration.Current.UmbracoSettings.Content.UseLegacyXmlSchema)
                    fileByteArray = this.Webservice.fetchPackage(packageGuid);
                else
                    fileByteArray = this.Webservice.fetchPackageByVersion(packageGuid, Version.Version41);
            }
            else
            {
                fileByteArray = this.Webservice.fetchProtectedPackage(packageGuid, key);
            }

            //successfull 
            if (fileByteArray.Length > 0)
            {

                // Check for package directory
                if (!System.IO.Directory.Exists(IOHelper.MapPath(packager.Settings.PackagerRoot)))
                    System.IO.Directory.CreateDirectory(IOHelper.MapPath(packager.Settings.PackagerRoot));


                System.IO.FileStream fs1 = null;
                fs1 = new FileStream(IOHelper.MapPath(packager.Settings.PackagerRoot + System.IO.Path.DirectorySeparatorChar.ToString() + packageGuid + ".umb"), FileMode.Create);
                fs1.Write(fileByteArray, 0, fileByteArray.Length);
                fs1.Close();
                fs1 = null;

                return "packages\\" + packageGuid + ".umb";

            }
            else
            {

                return "";
            }
        }
        
    }
}
