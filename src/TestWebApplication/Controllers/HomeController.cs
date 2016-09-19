using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Web;
using System.Web.Mvc;
using TestWcfService;

namespace TestWebApplication.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            string response = String.Empty;
            string exception = String.Empty;
            try
            {
                //creating the channel and calling it

                WS2007FederationHttpBinding federationBinding = new WS2007FederationHttpBinding();
                federationBinding.Security.Mode = WSFederationHttpSecurityMode.TransportWithMessageCredential;
                federationBinding.Security.Message.EstablishSecurityContext = false;
             
                EndpointAddress wcfServiceEndpoint = new EndpointAddress(Program.WCFAddress);
                ChannelFactory<ITestService> factory = new ChannelFactory<ITestService>(federationBinding, wcfServiceEndpoint);
                factory.Credentials.SupportInteractive = false;
                factory.Credentials.UseIdentityConfiguration = true;

                 var token = GetSecurityToken();

                // Create a channel.
                ITestService client = factory.CreateChannelWithIssuedToken(token);
                response = client.SayHello();
                ((IClientChannel)client).Close();

            }
            catch(Exception ex)
            {
                response = ex.Message;
                exception = ex.StackTrace;
            }

            ViewBag.ServiceResult = response;
            ViewBag.Exception = exception;


            return View();
        }

       
        public SecurityToken GetSecurityToken()
        {

            //service identifier whitin the ADFS server where I want to access, must be configured as RP in ADFS
            //this is the same value we configure on server side for the parameter audienceUris
            EndpointReference serviceAddress = new EndpointReference(@"http://testWCFService.gianlucb.local");

            //who gives me the token
            string stsAddress;

            //ignores certificates error
            ServicePointManager.ServerCertificateValidationCallback = (x, y, z, w) => true;

            //USERNAME
            stsAddress = @"https://sts.gianlucb.local/adfs/services/trust/13/usernamemixed";
            WS2007HttpBinding stsBinding = new WS2007HttpBinding(SecurityMode.TransportWithMessageCredential);
            stsBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Ntlm;
            stsBinding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;
            stsBinding.Security.Message.EstablishSecurityContext = false;

            WSTrustChannelFactory trustChannelFactory = new WSTrustChannelFactory(stsBinding, stsAddress);
            trustChannelFactory.Credentials.SupportInteractive = false;
            trustChannelFactory.Credentials.UserName.Password = "XXXXXXXXX";
            trustChannelFactory.Credentials.UserName.UserName = "DOMAIN\\USER";

            //---------------------

            //connection
            WSTrustChannel channel = (WSTrustChannel)trustChannelFactory.CreateChannel();

            RequestSecurityToken rst = new RequestSecurityToken(RequestTypes.Issue);
            rst.AppliesTo = serviceAddress;
            rst.KeyType = KeyTypes.Symmetric;
         
            RequestSecurityTokenResponse rstr = null;
            SecurityToken token = channel.Issue(rst, out rstr);
            var xmlSecurityToken = token as GenericXmlSecurityToken;

            Trace.WriteLine("Received the token:");
            Trace.WriteLine(xmlSecurityToken.TokenXml.InnerXml);

            return token;
        }

    }
}