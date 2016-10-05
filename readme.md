# WIF EXAMPLE #1 - ACTIVE SCENARIO WITH WCF SECURED SERVICE
This example uses WIF with .NET 4.5 to demonstrate the active scenario where a client call an STS to get a token for a target WCF service. There are three actors here: the client (WEB application), the WCF service and the ADFS server (STS).
The WCF service is secured with WIF/ADFS, so the client must present a valid token to access the service methods.  
Active scenario means the client (WEB) have to retrieve the token from the ADFS server (STS) itself. The token retrieved can then be used to call the WCF service.

*Notes:*
* By Default WCF uses **symmetric keys** for the tokens so we need a certificate to encrypt the token at ADFS side
    * the certificate must be configured as SSL certificate for the service (installed with private key on the machine store) --> it will be used to decrypt the token  
    * the public key (.cer) must be set as _encryption certificate_ in the ADFS server (for this relying party only)
* The token we receive is then **encrypted** with the WCF certificate and **signed** with the **ADFS Server signing certificate** --> this is why we also need to set the ADFS server signing certificate thumbprint in the WIF configuration
* The token used is SAML1.1 symmetric, NOT Bearer
    * if you want to use a Bearer token just change the IssuedKeyType property, in such case then you don't need to set the encryption certificate anymore
* This sample uses the simple username endpoint to retrieve the token from ADFS (means username and password are set explicitly on client side - WEB)
* In this scenario every web users will access the WCF service with the same credentials (hardcoded during the token request) --> this is not a delegation/impersonation scenario

## PREREQUISITES 
* ADSF server (STS)
* Domain Controller
             
## SETUP
1. register this WCF service with ADFS --> configure a new **Relying Party**
    * **Relying party identifier** = fantasy name (url) that identifies this service, the name used here must be set also on client side (as **EndpointReference**) and service side (as **AudienceUri**)
    * **Encryption** = select the certificate (.cer) used by this service (the same used to listen for HTTPS). We need this becasue WCF by default use symmetric tokens. If you switch to Bearer then this step is not necessary
    * Add some claim rules (i.e: copy AD attributes to Claims)
2. because we are doing selfhost for WCF service we must manually bind the certificate for SSL (_these steps are not required if IIS is used to host the service_): 
    * *netsh http add sslcert ipport=0.0.0.0:9999 certhash=thumbprint_of_ssl_server_certificate appid={XXXXX-XXXXX-XXXXXX-XXXXX-XXXX-XXXXXX}*      
    * *netsh http add urlacl url=https://+:9999/ user=EVERYONE* 
3. Change the code and set the right certificate thumbprint for _IssuerNameRegistry_ (certificate of the STS signing certificate) and for the ServiceCertificate (cert used for SSL)
4. Host the website and the wcf service on the same machine (or change the WCF service endpoint address)
 