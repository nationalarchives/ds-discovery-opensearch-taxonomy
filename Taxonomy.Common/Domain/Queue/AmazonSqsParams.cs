using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using System;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public class AmazonSqsParams
    {
        public string QueueUrl { get; set; }
        public bool UseIntegratedSecurity { get; set; }
        public bool UseEC2Credentials    { get; set; }
        public string Region {get; set;}
        public string RoleArn { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string SessionToken { get; set; }
        public int MaxSize { get; set; }
        public int WaitMilliseconds { get; set; }
        public string Profile { get; set; }
        public bool AssumeRole {get;set;}

        public AWSCredentials GetCredentials(string roleSessionname)
        {
            AWSCredentials credentials = null;
            AWSCredentials aWSAssumeRoleCredentials = null;

            if (!this.UseIntegratedSecurity)
            {
                if (!String.IsNullOrEmpty(this.SessionToken))
                {
                    credentials = new SessionAWSCredentials(awsAccessKeyId: this.AccessKey, awsSecretAccessKey: this.SecretKey, this.SessionToken);
                }
                else
                {
                    credentials = new BasicAWSCredentials(accessKey: this.AccessKey, secretKey: this.SecretKey);
                }
            }
            else
            {
                if (this.UseEC2Credentials)
                {
                    // Must be running on an AWS EC2 instance for this to work:
                    credentials = new InstanceProfileAWSCredentials();
                }
                else
                {
                    var chain = new CredentialProfileStoreChain();

                    if (!chain.TryGetAWSCredentials(this.Profile, out credentials))
                    { 
                        throw new TaxonomyException("Unable to obtain AWS credentials for update queue SQS.");
                    }
                }
            }

            if (this.AssumeRole && !String.IsNullOrEmpty(this.RoleArn))
            {
                aWSAssumeRoleCredentials = new AssumeRoleAWSCredentials(credentials, this.RoleArn, roleSessionname);
                return aWSAssumeRoleCredentials;
            }
            else
            { return credentials; }
            
        }
    }
}
