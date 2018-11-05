using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using RestSharpHelper.OAuth1;

namespace TagUtil
{
    class DiscogsInterface
    {
        private readonly DiscogsClient.DiscogsClient discogsClient = null;
        private readonly DiscogsClient.DiscogsClient _DiscogsClient;
        private readonly OAuthCompleteInformation _OAuthCompleteInformation;

        public DiscogsInterface(string myToken )
        {
            if( discogsClient == null )
                AuthorizeToken(myToken);
//            _OAuthCompleteInformation = null;
            //new OAuthCompleteInformation("", "", "", "");
            //_DiscogsClient = new DiscogsClient.DiscogsClient(_OAuthCompleteInformation);
        }

        private static string GetToken(string url)
        {
            Console.WriteLine("Please authourize the application and enter the final key in the console");
            Process.Start(url);
            string tokenKey = Console.ReadLine();
            tokenKey = string.IsNullOrEmpty(tokenKey) ? null : tokenKey;
            return tokenKey;
        }

        private void AuthorizeUser()
        {
            var oAuthConsumerInformation = new OAuthConsumerInformation("", "");
            var discogsClient = new DiscogsClient.DiscogsAuthentifierClient(oAuthConsumerInformation);

            var aouth = discogsClient.Authorize(s => Task.FromResult(GetToken(s))).Result;

            Console.WriteLine($"{((aouth != null) ? "Success" : "Fail")}");
            Console.WriteLine($"Token:{aouth?.TokenInformation?.Token}, Token:{aouth?.TokenInformation?.TokenSecret}");
        }

        private void AuthorizeToken(string myToken )
        {
            //Create authentication based on Discogs token
            var tokenInformation = new DiscogsClient.Internal.TokenAuthenticationInformation(myToken);
            //Create discogs client using the authentication
            var discogsClient = new DiscogsClient.DiscogsClient(tokenInformation);
        }

    }
}
