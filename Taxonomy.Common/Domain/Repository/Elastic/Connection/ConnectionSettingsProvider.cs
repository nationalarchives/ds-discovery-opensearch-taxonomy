using Amazon;
using Amazon.Runtime;
using Amazon.SecurityToken.Model;

//using MongoDB.Driver.Core.Configuration;

//using MongoDB.Driver.Core.Configuration;
using Newtonsoft.Json;
using OpenSearch.Client;
using OpenSearch.Client.JsonNetSerializer;
using OpenSearch.Net;
using OpenSearch.Net.Auth.AwsSigV4;
using System;
using System.Configuration;
using System.Linq;
using static OpenSearch.Client.ConnectionSettings;

namespace NationalArchives.Taxonomy.Common.Domain.Repository.OpenSearch
{
    internal static class ConnectionSettingsProvider
    {
        private const string DATE_FORMAT = "yyyy-MM-dd";
        private const string ROLE_SESSION_NAME = "Elastic_Update";
        private const string AWS_ENV_CONFIG_EXCEPTION = "Unable to obtain one or more of the following AWS Components: Access Key, Secret Key, Role ARN, Region.  Please check your environment variables.";
        private const string AWS_UNKNOWN_REGION = "Unable to obtain AWS Region.  Please check the DISC_ELASTIC_UPDATE_AWS_REGION environment variable.";

        private static ConnectionSettings _connectionSettings;

        private static readonly object _locker = new object();

        public static ConnectionSettings GetConnectionSettings(OpenSearchConnectionParameters cParams)
        {

            lock (_locker)
            {
                if (_connectionSettings == null)
                {
                    OpenSearchConnectionMode connectionMode = cParams.OpenSearchAwsParams.OpenSearchConnectionMode;

                    SourceSerializerFactory serialiserFactory = (builtinJsonSerializerSettings, connectionSettingValues) =>
                       new JsonNetSerializer
                       (
                           builtinJsonSerializerSettings, connectionSettingValues,
                           () => new JsonSerializerSettings
                           {
                               DateFormatString = DATE_FORMAT
                           }
                       );

                    switch (connectionMode)
                    {
                        case OpenSearchConnectionMode.Agnostic:

                            using (IConnectionPool pool = new SingleNodeConnectionPool(cParams.Uri))
                            {
                                _connectionSettings = new ConnectionSettings(pool, serialiserFactory).DefaultIndex(cParams.IndexDatabase);
                            }

                            break;

                        case OpenSearchConnectionMode.AwsBasic:

                            string awsAccessKey = cParams.OpenSearchAwsParams.AccessKey;
                            string awsSecretKey = cParams.OpenSearchAwsParams.SecretKey;
                            string awsRoleArn = cParams.OpenSearchAwsParams.RoleArn;
                            string strRegion = cParams.OpenSearchAwsParams.Region;
                            string awsSessionToken = cParams.OpenSearchAwsParams.SessionToken;

                            if (new[] { awsAccessKey, awsSecretKey, awsRoleArn, strRegion }.Any(s => String.IsNullOrWhiteSpace(s)))
                            {
                                throw new ConfigurationErrorsException(AWS_ENV_CONFIG_EXCEPTION);
                            }

                            AWSCredentials credentials = null;

                            if (!String.IsNullOrEmpty(awsSessionToken))
                            {
                                credentials = new SessionAWSCredentials(awsAccessKeyId: awsAccessKey, awsSecretAccessKey: awsSecretKey, awsSessionToken);
                            }
                            else
                            {
                                credentials = new BasicAWSCredentials(accessKey: awsAccessKey, secretKey: awsSecretKey);
                            }

                            AWSCredentials aWSAssumeRoleCredentials = new AssumeRoleAWSCredentials(credentials, awsRoleArn, ROLE_SESSION_NAME);

                            RegionEndpoint awsRegion = RegionEndpoint.GetBySystemName(strRegion);
                            if (awsRegion.DisplayName.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                            {
                                throw new ConfigurationErrorsException(AWS_UNKNOWN_REGION);
                            }

                            using (IConnection httpConnection = new AwsSigV4HttpConnection(aWSAssumeRoleCredentials, awsRegion))
                            {
                                using (IConnectionPool pool = new SingleNodeConnectionPool(cParams.Uri))
                                {
                                    _connectionSettings = new ConnectionSettings(pool, httpConnection, serialiserFactory).DefaultIndex(cParams.IndexDatabase);
                                }
                            }

                            break;

                        case OpenSearchConnectionMode.EC2:

                            AWSCredentials ec2Credentials = null;
                            string roleArnEC2 = cParams.OpenSearchAwsParams.RoleArn;

                            if (!String.IsNullOrEmpty(roleArnEC2))
                            {
                                ec2Credentials = new InstanceProfileAWSCredentials(roleArnEC2);
                            }
                            else
                            {
                                ec2Credentials = new InstanceProfileAWSCredentials();
                            }

                            string strRegionEc2 = cParams.OpenSearchAwsParams.Region;
                            RegionEndpoint awsRegionEC2 = RegionEndpoint.GetBySystemName(strRegionEc2);
                            if (awsRegionEC2.DisplayName.Equals(AWS_UNKNOWN_REGION, StringComparison.OrdinalIgnoreCase))
                            {
                                throw new ConfigurationErrorsException(AWS_ENV_CONFIG_EXCEPTION);
                            }

                            using (IConnection httpConnection = new AwsSigV4HttpConnection(ec2Credentials, awsRegionEC2))
                            {
                                using (IConnectionPool pool = new SingleNodeConnectionPool(cParams.Uri))
                                {
                                    _connectionSettings = new ConnectionSettings(pool, httpConnection, serialiserFactory).DefaultIndex(cParams.IndexDatabase);
                                }
                            }

                            break;

                        default:
                            throw new ConfigurationErrorsException("Invalid OpenSearch Connection mode!");
                    }
                } 
            }


            //if (cParams.OpenSearchAwsParams?.UseAwsConnection ?? false)
            //{
            //    string awsAccessKey = cParams.OpenSearchAwsParams.AccessKey;
            //    string awsSecretKey = cParams.OpenSearchAwsParams.SecretKey;
            //    string awsRoleArn = cParams.OpenSearchAwsParams.RoleArn;
            //    string strRegion = cParams.OpenSearchAwsParams.Region;

            //    if (new[] { awsAccessKey, awsSecretKey, awsRoleArn, strRegion }.Any(s => String.IsNullOrWhiteSpace(s)))
            //    {
            //        throw new ConfigurationErrorsException(AWS_ENV_CONFIG_EXCEPTION);
            //    }

            //    AWSCredentials basicCredentials = new BasicAWSCredentials(accessKey: awsAccessKey, secretKey: awsSecretKey);
            //    AWSCredentials aWSAssumeRoleCredentials = new AssumeRoleAWSCredentials(basicCredentials, awsRoleArn, ROLE_SESSION_NAME);

            //    RegionEndpoint awsRegion = RegionEndpoint.GetBySystemName(strRegion);
            //    if (awsRegion.DisplayName.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            //    {
            //        throw new ConfigurationErrorsException(AWS_UNKNOWN_REGION);
            //    }

            //    using (IConnection httpConnection = new AwsSigV4HttpConnection(aWSAssumeRoleCredentials, awsRegion))
            //    {
            //        using (IConnectionPool pool = new SingleNodeConnectionPool(cParams.Uri))
            //        {
            //            _connectionSettings = new ConnectionSettings(pool, httpConnection,
            //                (builtinJsonSerializerSettings, connectionSettingValues) =>
            //                    new JsonNetSerializer(
            //                        builtinJsonSerializerSettings, connectionSettingValues,
            //                        () => new JsonSerializerSettings
            //                        {
            //                            DateFormatString = DATE_FORMAT
            //                        }
            //                        )).DefaultIndex(cParams.IndexDatabase);
            //        }
            //    }
            //}
            //else
            //{
            //    using (IConnectionPool pool = new SingleNodeConnectionPool(cParams.Uri))
            //    {
            //        _connectionSettings = new ConnectionSettings(pool,
            //            (builtinJsonSerializerSettings, connectionSettingValues) =>
            //                new JsonNetSerializer(
            //                    builtinJsonSerializerSettings, connectionSettingValues,
            //                    () => new JsonSerializerSettings
            //                    {
            //                        DateFormatString = DATE_FORMAT
            //                    }
            //                    )).DefaultIndex(cParams.IndexDatabase);
            //    }
            //}
            return _connectionSettings;

        }

        private sealed class Destructor
        {
            ~Destructor()
            {
                ((IDisposable)_connectionSettings)?.Dispose();
            }
        }
    }
}
