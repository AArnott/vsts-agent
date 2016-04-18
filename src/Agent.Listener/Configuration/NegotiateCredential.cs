using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Net;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public sealed class NegotiateCredential : CredentialProvider
    {
        public NegotiateCredential() : base(Constants.Configuration.Negotiate) { }

        public override VssCredentials GetVssCredentials(IHostContext context)
        {
            Tracing trace = context.GetTrace(nameof(NegotiateCredential));
            trace.Info(nameof(GetVssCredentials));

            if (CredentialData == null || !CredentialData.Data.ContainsKey("Username")
                || !CredentialData.Data.ContainsKey("Password") || !CredentialData.Data.ContainsKey("Url"))
            {
                throw new InvalidOperationException("Must call ReadCredential first.");
            }

            string username = CredentialData.Data["Username"];
            trace.Info($"username retrieved: {username.Length} chars");

            string password = CredentialData.Data["Password"];
            trace.Info($"password retrieved: {password.Length} chars");

            //create Negotiate and NTLM credentials
            var credential = new NetworkCredential(username, password);
            var credentialCache = new CredentialCache();
            var serverUrl = new Uri(CredentialData.Data["Url"]);
            switch (Constants.Agent.Platform)
            {
                case Constants.OSPlatform.Linux:                    
                case Constants.OSPlatform.OSX:
                    credentialCache.Add(serverUrl, "NTLM", credential);
                    break;
                case Constants.OSPlatform.Windows:
                    credentialCache.Add(serverUrl, "Negotiate", credential);
                    break;
            }            
            
            VssCredentials creds = new VssClientCredentials(new WindowsCredential(credentialCache));

            trace.Verbose("cred created");

            return creds;
        }

        public override void ReadCredential(IHostContext context, Dictionary<string, string> args, bool enforceSupplied)
        {
            var promptManager = context.GetService<IPromptManager>();
            CredentialData.Data["Username"] = promptManager.ReadValue(CliArgs.UserName,
                                            StringUtil.Loc("NTLMUsername"),
                                            false,
                                            String.Empty,
                                            //TODO: use Validators.NTAccountValidator when it works on Linux
                                            Validators.NonEmptyValidator,
                                            args,
                                            enforceSupplied);

            CredentialData.Data["Password"] = promptManager.ReadValue(CliArgs.Password,
                                            StringUtil.Loc("NTLMPassword"),
                                            true,
                                            String.Empty,
                                            Validators.NonEmptyValidator,
                                            args,
                                            enforceSupplied);

            CredentialData.Data["Url"] = args[CliArgs.Url];
        }
    }
}