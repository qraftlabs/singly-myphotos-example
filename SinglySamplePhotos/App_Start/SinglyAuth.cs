using System;
using Microsoft.Web.WebPages.OAuth;
using SinglySamplePhotos.Singly;
using System.Collections.Generic;

[assembly: WebActivator.PreApplicationStartMethod(
    typeof(SinglySamplePhotos.App_Start.SinglyAuth), "PreStart")]

namespace SinglySamplePhotos.App_Start {
    public static class SinglyAuth {
        public static void PreStart() {
            
            var provider = "twitter"; //eg: twitter
            var clientId = "e6abf70f5defd779caf23dc4ac22fc7f"; //get this from singly.com.
            var clientSecret = "ed6c0f960b8c364f32d5099ef2d2f14f"; //get this from singly.com.
            
            var singlyClient = SinglyClient.Create(provider, clientId, clientSecret);
            OAuthWebSecurity.RegisterClient(singlyClient, "twitter", new Dictionary<string, object>());
        }
    }
}