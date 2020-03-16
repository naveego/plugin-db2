using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginDb2.Helper;
using Xunit;
using Record = Naveego.Sdk.Plugins.Record;

namespace PluginDb2.Plugin
{
    public class PluginIntegrationTest
    {
        private Settings GetSettings()
        {
            return new Settings
            {
                Server = "150.136.152.223",
                Database = "TESTDB",
                Username = "DB2INST1",
                Password = "test123"
            };
        }

        private ConnectRequest GetConnectSettings()
        {
            var settings = GetSettings();

            return new ConnectRequest
            {
                SettingsJson = JsonConvert.SerializeObject(settings),
                OauthConfiguration = new OAuthConfiguration(),
                OauthStateJson = ""
            };
        }

        private Schema GetTestSchema(string id = "test", string name = "test", string query = "")
        {
            return new Schema
            {
                Id = id,
                Name = name,
                Query = query
            };
        }

        public PluginIntegrationTest()
        {
            Setup.EnsureEnvironment();
        }

        [Fact]
        public async Task ConnectSessionTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginDb2.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var request = GetConnectSettings();
            var disconnectRequest = new DisconnectRequest();

            // act
            var response = client.ConnectSession(request);
            var responseStream = response.ResponseStream;
            var records = new List<ConnectResponse>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
                client.Disconnect(disconnectRequest);
            }

            // assert
            Assert.Single(records);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ConnectTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginDb2.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var request = GetConnectSettings();

            // act
            var response = client.Connect(request);

            // assert
            Assert.IsType<ConnectResponse>(response);
            Assert.Equal("", response.SettingsError);
            Assert.Equal("", response.ConnectionError);
            Assert.Equal("", response.OauthError);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasAllTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginDb2.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
                SampleSize = 10
            };

            // act
            client.Connect(connectRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.True(response.Schemas.Count > 0);

            var firstSchema = response.Schemas.First();
            Assert.True(firstSchema.Properties.Count > 0);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        /*
        [Fact]
        public async Task DiscoverSchemasRefreshTableTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginDb2.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                SampleSize = 10,
                ToRefresh = {GetTestSchema("`classicmodels`.`customers`", "classicmodels.customers")}
            };

            // act
            client.Connect(connectRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Single(response.Schemas);

            var schema = response.Schemas[0];
            Assert.Equal($"`classicmodels`.`customers`", schema.Id);
            Assert.Equal("classicmodels.customers", schema.Name);
            Assert.Equal($"", schema.Query);
            Assert.Equal(10, schema.Sample.Count);
            Assert.Equal(13, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal("`customerNumber`", property.Id);
            Assert.Equal("customerNumber", property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.Integer, property.Type);
            Assert.True(property.IsKey);
            Assert.False(property.IsNullable);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        

        [Fact]
        public async Task DiscoverSchemasRefreshQueryTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginDb2.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                SampleSize = 10,
                ToRefresh = {GetTestSchema("test", "test", $"SELECT * FROM `classicmodels`.`customers`")}
            };

            // act
            client.Connect(connectRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Single(response.Schemas);

            var schema = response.Schemas[0];
            Assert.Equal($"test", schema.Id);
            Assert.Equal("test", schema.Name);
            Assert.Equal($"SELECT * FROM `classicmodels`.`customers`", schema.Query);
            Assert.Equal(10, schema.Sample.Count);
            Assert.Equal(13, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal("`customerNumber`", property.Id);
            Assert.Equal("customerNumber", property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.Integer, property.Type);
            Assert.True(property.IsKey);
            Assert.False(property.IsNullable);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        */
        
        [Fact]
        public async Task ReadStreamTableSchemaTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginDb2.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            
            var request = new ReadRequest()
            {
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
            };

            // act
            client.Connect(connectRequest);
            var schemas = client.DiscoverSchemas(new DiscoverSchemasRequest());

            request.Schema = schemas.Schemas.First();
            
            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Record>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            Assert.Equal(9, records.Count);

            var firstRecord = records.First();
            //  BUSINESS_PARTNER 	INSURED    	MARKET 	MARKET_COMPANY 	RETAIL_BROKER 	RETAIL_BROKER_COMPANY 	BUSINESS_PARTNER_NAME 	BUSINESS_PARTNER_STATUS 	BUSINESS_PARTNER_TYPE 	ACCOUNT_STREET         	ACCOUNT_CITY 	ACCOUNT_STATE 	ACCOUNT_ZIP_CODE 	ACCOUNT_COUNTRY 	ACCOUNT_PHONE 	ACCOUNT_FAX 	OFFICE_STREET          	OFFICE_CITY 	OFFICE_STATE 	OFFICE_ZIP_CODE 	OFFICE_COUNTRY 	OFFICE_PHONE 	OFFICE_FAX 	COMPANY 	ENTERPRISE 	FIRST_MODIFIED  	FIRST_AUDIT_ID 	LAST_MODIFIED   	AUDIT_ID 	INVOICE_STREET         	INVOICE_CITY 	INVOICE_STATE 	INVOICE_ZIP_CODE 	INVOICE_COUNTRY 	INVOICE_PHONE 	INVOICE_FAX 	STATEMENT_STREET       	STATEMENT_CITY 	STATEMENT_STATE 	STATEMENT_ZIP_CODE 	STATEMENT_COUNTRY 	STATEMENT_PHONE 	STATEMENT_FAX 	PREMISES_STREET        	PREMISES_CITY 	PREMISES_STATE 	PREMISES_ZIP_CODE 	PREMISES_COUNTRY 	PREMISES_PHONE 	PREMISES_FAX 	MAIL_STREET            	MAIL_CITY  	MAIL_STATE 	MAIL_ZIP_CODE 	MAIL_COUNTRY 	MAIL_PHONE 	MAIL_FAX 	OFFICE_COUNTY 	ACCOUNT_STREET_LINE1   	ACCOUNT_STREET_LINE2 	INVOICE_STREET_LINE1   	INVOICE_STREET_LINE2 	STATEMENT_STREET_LINE1 	STATEMENT_STREET_LINE2 	PREMISES_STREET_LINE1  	PREMISES_STREET_LINE2 	MAIL_STREET_LINE1      	MAIL_STREET_LINE2 	OFFICE_STREET_LINE1    	OFFICE_STREET_LINE2 //
            //  ---------------- 	---------- 	------ 	-------------- 	------------- 	--------------------- 	--------------------- 	----------------------- 	--------------------- 	---------------------- 	------------ 	------------- 	---------------- 	--------------- 	------------- 	----------- 	---------------------- 	----------- 	------------ 	--------------- 	-------------- 	------------ 	---------- 	------- 	---------- 	--------------- 	-------------- 	--------------- 	-------- 	---------------------- 	------------ 	------------- 	---------------- 	--------------- 	------------- 	----------- 	---------------------- 	-------------- 	--------------- 	------------------ 	----------------- 	--------------- 	------------- 	---------------------- 	------------- 	-------------- 	----------------- 	---------------- 	-------------- 	------------ 	---------------------- 	---------- 	---------- 	------------- 	------------ 	---------- 	-------- 	------------- 	---------------------- 	-------------------- 	---------------------- 	-------------------- 	---------------------- 	---------------------- 	---------------------- 	--------------------- 	---------------------- 	----------------- 	---------------------- 	-------------------
            //  1000060211	1000223203	  NULL	          NULL	   1000001012	           1000005201	XYZ Insurance Agency 	Active                 	Retail_Broker        	904 Riverview St      	Warminster  	PA           	18974           	USA            	8042888100   	           	904 Riverview St      	Warminster 	PA          	18974          	USA           	8042888100  	          	   3456	      1000	2008-12-06 9:35	TAPL1         	2018-12-11 7:55	ADMIN   	904 Riverview St      	Warminster  	PA           	18974           	USA            	8042888100   	           	904 Riverview St      	Warminster    	PA             	18974             	USA              	8042888100     	             	904 Riverview St      	Warminster   	PA            	18974            	USA             	8042888100    	            	904 Riverview St      	Warminster	PA        	18974        	USA         	8042888100	        	NULL         	904 Riverview St      	NULL                	904 Riverview St      	NULL                	904 Riverview St      	NULL                  	904 Riverview St      	NULL                 	904 Riverview St      	NULL             	904 Riverview St      	NULL

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            Assert.Equal((long)1000060211, record["BUSINESS_PARTNER"]);
            Assert.Equal("XYZ Insurance Agency", record["BUSINESS_PARTNER_NAME"]);
            
            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        
        [Fact]
        public async Task ReadStreamQuerySchemaTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginDb2.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var schema = GetTestSchema("test", "test", $"SELECT * FROM `classicmodels`.`orders`");
            
            var connectRequest = GetConnectSettings();

            var schemaRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = {schema}
            };

            var request = new ReadRequest()
            {
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
            };

            // act
            client.Connect(connectRequest);
            var schemasResponse = client.DiscoverSchemas(schemaRequest);
            request.Schema = schemasResponse.Schemas[0];
            
            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Record>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            Assert.Equal(326, records.Count);

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            Assert.Equal((long)10100, record["`orderNumber`"]);
            Assert.Equal(DateTime.Parse("2003-01-06"), record["`orderDate`"]);
            Assert.Equal(DateTime.Parse("2003-01-13"), record["`requiredDate`"]);
            Assert.Equal(DateTime.Parse("2003-01-10"), record["`shippedDate`"]);
            Assert.Equal("Shipped", record["`status`"]);
            Assert.Equal("", record["`comments`"]);
            Assert.Equal((long)363, record["`customerNumber`"]);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        
        [Fact]
        public async Task ReadStreamLimitTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginDb2.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var schema = GetTestSchema("`classicmodels`.`customers`", "classicmodels.customers");
            
            var connectRequest = GetConnectSettings();

            var schemaRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = {schema}
            };

            var request = new ReadRequest()
            {
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
                Limit = 10
            };

            // act
            client.Connect(connectRequest);
            var schemasResponse = client.DiscoverSchemas(schemaRequest);
            request.Schema = schemasResponse.Schemas[0];
            
            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Record>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            Assert.Equal(10, records.Count);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
    }
}