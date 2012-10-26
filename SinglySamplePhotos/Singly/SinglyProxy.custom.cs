using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SinglySamplePhotos.Singly
{
    public partial class SinglyContext
    {
        
        public SinglyContext()
            : this(new Uri("http://localhost:8778/types"))
        {}
        
        partial void OnContextCreated()
        {
            this.UseJsonFormatWithDefaultServiceModel();
            this.IgnoreMissingProperties = true;
            this.SendingRequest += new EventHandler<System.Data.Services.Client.SendingRequestEventArgs>(SinglyContext_SendingRequest);    
        }

        void SinglyContext_SendingRequest(object sender, System.Data.Services.Client.SendingRequestEventArgs e)
        {
            if(System.Web.HttpContext.Current != null && System.Web.HttpContext.Current.Session["singly_accesstoken"] != null){
                e.RequestHeaders.Add("Authorization", "Bearer " + System.Web.HttpContext.Current.Session["singly_accesstoken"]);
            }
        }
    }
}