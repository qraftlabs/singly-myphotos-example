using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;
using System.Linq;
using DotNetOpenAuth.AspNet;
using DotNetOpenAuth.AspNet.Clients;
using DotNetOpenAuth.Messaging;
using Microsoft.Web.WebPages.OAuth;
using Newtonsoft.Json.Linq;

namespace SinglySamplePhotos.Singly
{
    /// <summary>
    /// Singly OAuth client.
    /// This client uses DotNetOpenAuth. For more information on getting
    /// the clientId and clientSecret, please to go http://singly.com.
    /// </summary>
    public class SinglyClient : OAuth2Client
    {
        #region Constants and Fields

        private const string BaseEndpoint = "https://api.singly.com";

        /// <summary>
        /// The authorization endpoint.
        /// </summary>
        private const string AuthorizationEndpoint = "https://api.singly.com/oauth/authorize";

        /// <summary>
        /// The token endpoint.
        /// </summary>
        private const string TokenEndpoint = "https://api.singly.com/oauth/access_token";

        /// <summary>
        /// The profile endpoint for getting the user's data.
        /// </summary>
        private const string ProfileEndpoint = "https://api.singly.com/profile";


        /// <summary>
        /// The profiles endpoint for getting the user's data.
        /// </summary>
        private const string ProfilesEndpoint = "https://api.singly.com/v0/profiles";

        /// <summary>
        /// The client id.
        /// </summary>
        private readonly string clientId;

        /// <summary>
        /// The client secret.
        /// </summary>
        private readonly string clientSecret;

        /// <summary>
        /// Which singly service would you like to use to authenticate.
        /// </summary>
        public string Service { get; private set; }

        #endregion

        public static SinglyClient Create(string service, string clientId, string clientSecret)
        {
            Current = new SinglyClient(service, clientId, clientSecret);
            return Current;
        }

        public static SinglyClient Current { get; private set; }

        /// <summary>
        /// Singly OAuth2 client.
        /// </summary>
        /// <param name="service">Singly service with which the user will authenticate.</param>
        /// <param name="clientId">Singly client id.</param>
        /// <param name="clientSecret">Singly client secret.</param>
        private SinglyClient(string service, string clientId, string clientSecret)
			: base("Singly - " + service) {

            if (string.IsNullOrWhiteSpace(service)) throw new ArgumentNullException("service");
            if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentNullException("clientId");
            if (string.IsNullOrWhiteSpace(clientSecret)) throw new ArgumentNullException("clientSecret");

			this.clientId = clientId;
			this.clientSecret = clientSecret;
            this.Service = service;
		}

        /// <summary>
        /// Should return the Singly url for authentication.
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        protected override Uri GetServiceLoginUrl(Uri returnUrl)
        {
            return GetServiceLoginUrl2(returnUrl);
        }

        private Uri GetServiceLoginUrl2(Uri returnUrl, string service = null)
        {
            var builder = new UriBuilder(AuthorizationEndpoint);
            builder.AppendQueryArgument("client_id", this.clientId);
            builder.AppendQueryArgument("redirect_uri", returnUrl.AbsoluteUri);
            builder.AppendQueryArgument("service", service ?? Service);
            var session = HttpContext.Current.Session;
            if (session["account_id"] != null && session["singly_accesstoken"] != null)
            {
                builder.AppendQueryArgument("account", session["account_id"].ToString());
                builder.AppendQueryArgument("access_token", session["singly_accesstoken"].ToString());
            }
            return builder.Uri;
        }

        /// <summary>
        /// Return the data from calling Singly's profile url.
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        protected override IDictionary<string, string> GetUserData(string accessToken)
        {
            JObject jObj;
            var request = WebRequest.Create(ProfileEndpoint + "?access_token=" + accessToken);
            
            using (var response = request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    var streamReader = new StreamReader(responseStream);
                    jObj = JObject.Parse(streamReader.ReadToEnd());
                }
            }

            var userData = new Dictionary<string, string>();
            userData.Add("id", jObj["id"].ToString());
            userData.Add("url", jObj["url"].ToString());
            userData.Add("handle", jObj["handle"].ToString());
            userData.Add("description", jObj["description"].ToString());
            userData.Add("thumbnail_url", jObj["thumbnail_url"].ToString());
            userData.Add("name", jObj["name"].ToString());

            return userData;
        }

        /// <summary>
        /// Get's the access token that should be included in every
        /// secured API call.
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <param name="authorizationCode"></param>
        /// <returns>Access Token or null if there was a problem.</returns>
        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
        {
            var entity = new StringBuilder()
                .Append(string.Format("client_id={0}&", clientId))
                .Append(string.Format("client_secret={0}&", clientSecret))
                .Append(string.Format("code={0}", authorizationCode))
                .ToString();

            WebRequest tokenRequest = WebRequest.Create(TokenEndpoint);
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            tokenRequest.ContentLength = entity.Length;
            tokenRequest.Method = "POST";

            using (Stream requestStream = tokenRequest.GetRequestStream())
            {
                var writer = new StreamWriter(requestStream);
                writer.Write(entity);
                writer.Flush();
            }

            HttpWebResponse tokenResponse = (HttpWebResponse)tokenRequest.GetResponse();
            if (tokenResponse.StatusCode == HttpStatusCode.OK)
            {
                using (Stream responseStream = tokenResponse.GetResponseStream())
                {
                    
                    var serializer = new DataContractJsonSerializer(typeof(OAuth2AccessTokenData));
                    var tokenData = (OAuth2AccessTokenData)serializer.ReadObject(responseStream);
                    if (tokenData != null)
                    {
                        return tokenData.AccessToken;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Override in order to save the access_token in session.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="returnPageUrl"></param>
        /// <returns></returns>
        public override AuthenticationResult VerifyAuthentication(HttpContextBase context, Uri returnPageUrl)
        {
            var result = base.VerifyAuthentication(context, returnPageUrl);

            context.Session["singly_accesstoken"] = result.ExtraData["accesstoken"];
            
            return result;
        }


        public void AddServiceToProfile(string service, Uri callbackUrl)
        {
            var url = GetServiceLoginUrl2(callbackUrl, service);
            HttpContext.Current.Response.Redirect(url.ToString(), true);
        }

        public Profile GetProfile() 
        {
            if (HttpContext.Current.Session["singly_accesstoken"] == null) 
            {
                throw new InvalidOperationException("unable to query for profiles, missing access_token");
            }

            JObject jObj;
            var request = WebRequest.Create(ProfilesEndpoint + "?access_token=" + HttpContext.Current.Session["singly_accesstoken"]);

            using (var response = request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    var streamReader = new StreamReader(responseStream);
                    jObj = JObject.Parse(streamReader.ReadToEnd());
                }
            }

            var id = jObj["id"].ToString();
            var services = jObj.Where<KeyValuePair<string, JToken>>(kv => kv.Key != "id")
                                .ToDictionary(kv => kv.Key, kv => kv.Value.OfType<JToken>().Select(jt => jt.ToString()).ToArray());
            
            return new Profile(id, services);
        }

        public class Profile 
        {
            public Profile(string id, IDictionary<string, string[]> services) 
            {
                Id = id;
                Services = services;
            }

            public string Id { get; private set; }
            public IDictionary<string, string[]> Services { get; private set; }
        }
    }
    
}
