using System;
using System.Configuration;
using PartnerServiceReference;
using MetadataServiceReference;


namespace tc
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                String serverUrl = ConfigurationManager.AppSettings.Get("serverurl");
                String username  = ConfigurationManager.AppSettings.Get("username");
                String password  = ConfigurationManager.AppSettings.Get("password");
                String orgId     = ConfigurationManager.AppSettings.Get("orgId");

                Console.WriteLine("Updating profile " + args[0] + " to hide tab " + args[1]);
                setTabVisibility(serverUrl, username, password, orgId, args[0], args[1], TabVisibility.Hidden );

                Console.WriteLine("Profile updated");
            }
            catch (Exception x)
            {
                Console.WriteLine("Failed to update profile: " + x.Message);
            }
        }

        /// <summary>
        /// Update the visibility of a tab for a given profile.
        /// </summary>
        /// <param name="serverUrl">URL to SF server</param>
        /// <param name="username">Login username</param>
        /// <param name="password">Login password</param>
        /// <param name="orgId">Organization ID found at settings/company settings/company information/salesforce.com organization id</param>
        /// <param name="profileName">API name of profile to update</param>
        /// <param name="tabName">SF name of the tab whose visiblity status to update.  Note this is not the same as the label given in the SF CRM.  For instance,
        /// the standard objects such as 'Account' will have the name 'standard-Account'.</param>
        /// <param name="tabVisibility">The visibility status to be assigned for the given tab for the given profile.</param>
        static void setTabVisibility(String serverUrl, String username, String password, String orgId, String profileName, String tabName, TabVisibility tabVisibility)
        {
            // establish session
            SoapClient client = new SoapClient();
            LoginResult res = client.login(new PartnerServiceReference.LoginScopeHeader { organizationId = orgId }, null, username, password);
            var sessionId = res.sessionId;
            // Console.WriteLine("Session ID: " + sessionId);

            try
            {
                // get the profile and the tabs
                MetadataPortTypeClient mclient = new MetadataPortTypeClient(MetadataPortTypeClient.EndpointConfiguration.Metadata, res.metadataServerUrl);
                Metadata[] profiles = mclient.readMetadata(new MetadataServiceReference.SessionHeader { sessionId = sessionId }, null, "Profile", new String[] { profileName });
                if (profiles.Length != 1)
                    throw new Exception("Failed to locate profile");

                // set the visiblity for the tab
                Profile profile = (Profile)profiles[0];
                Profile updatedProfile = null;
                foreach (ProfileTabVisibility tabVisibilityField in profile.tabVisibilities)
                {
                    // Console.WriteLine(tabVisibilityField.tab);
                    if (tabVisibilityField.tab == tabName)
                    {
                        updatedProfile = new Profile() { fullName = profile.fullName };
                        updatedProfile.tabVisibilities = new ProfileTabVisibility[] {
                            new ProfileTabVisibility()
                            {
                                tab = tabName,
                                visibility = tabVisibility
                            }
                        };
                        break;
                    }
                }
                if (null == updatedProfile)
                    throw new Exception("Failed to locate tab");

                // update the profile
                MetadataServiceReference. SaveResult[] response = mclient.updateMetadata(new MetadataServiceReference.SessionHeader { sessionId = sessionId }, null, null, new Metadata[] { updatedProfile });
                if ( response.Length != 1 || !response[0].success )
                    throw new Exception("Failed to update profile");
            }
            finally
            {
                // close session
                client = new SoapClient(SoapClient.EndpointConfiguration.Soap, res.serverUrl);
                client.logout(new PartnerServiceReference.SessionHeader { sessionId = sessionId }, null);
            }
        }
    }
}
