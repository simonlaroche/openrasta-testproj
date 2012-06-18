using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using OpenRasta.Configuration;
using OpenRasta.Hosting.InMemory;
using OpenRasta.Web;
using Simple.Testing.ClientFramework;

namespace restservice.tests
{
    public class SpecificationX
    {
        public Specification Get_my_resource()
        {
            return new RestSpecification<MyResource>
                       {
                           Accept = MediaType.Json,
                           Method = "Get",
                           Uri = "http://localhost/MyResource",
                           Expect =
                               {
                                   x => x.StatusCode == HttpStatusCode.OK,
                                   x => x.Resource.Name == "foobar"
                               }
                       };


        }

        public Specification Get_my_resource_not_acceeptable_media_type()
        {
            return new RestSpecification<MyResource>
            {
                Accept = MediaType.TextPlain,
                Method = "Get",
                Uri = "http://localhost/MyResource",
                Expect =
                               {
                                   x => x.StatusCode == HttpStatusCode.NotAcceptable,
                               }
            };
        }
    }

    public static class ResponseExtension
    {
    }

    public class RestSpecification<TResource> : QuerySpecification<InMemoryHost, Result<TResource>>
    {
        public MediaType Accept;
        public string Method;
        public string Uri;

        public RestSpecification()
        {
            On = () => new InMemoryHost(new Configuration());
            When = x => ToResult(ProcessRequest(x));
        }

        private Result<TResource> ToResult(IResponse response)
        {
            DataContractJsonSerializer serializer = null;
            if (response.Headers.ContainsKey("Content-Type"))
            {
                var contentType = new MediaType(response.Headers["Content-Type"]);

                if (contentType.Equals(MediaType.Json))
                {
                    serializer = new DataContractJsonSerializer(typeof (TResource));
                }
                else if (Accept.Equals(MediaType.Xml))
                {
                    serializer = new DataContractJsonSerializer(typeof (TResource));
                }
            }

            if (serializer == null)
            {
                return new Result<TResource>(default(TResource), response);
            }

            return new Result<TResource>((TResource) serializer.ReadObject(response.Entity.Stream), response);
        }

        private IResponse ProcessRequest(InMemoryHost x)
        {
            IResponse response = x.ProcessRequest(BuildRequest());
            response.Entity.Stream.Seek(0, SeekOrigin.Begin);
            return response;
        }

        private IRequest BuildRequest()
        {
            var request = new InMemoryRequest {HttpMethod = Method, Uri = new Uri(Uri)};
            request.Entity.Headers["Accept"] = Accept.ToString();
            return request;
        }
    }


    public class Result<TResource>
    {
        private readonly TResource resource;
        private readonly IResponse response;

        public Result(TResource resource, IResponse response)
        {
            this.resource = resource;
            this.response = response;
        }

        public TResource Resource
        {
            get { return resource; }
        }

        public HttpStatusCode StatusCode
        {
            get { return (HttpStatusCode) response.StatusCode; }
        }

        public IResponse Response
        {
            get { return response; }
        }
    }

    public class Configuration : IConfigurationSource
    {
        #region IConfigurationSource Members

        public void Configure()
        {
            using (OpenRastaConfiguration.Manual)
            {
                ResourceSpace.Has.ResourcesOfType<MyResource>()
                    .AtUri("/MyResource")
                    .HandledBy<MyResourceHandler>().AsJsonDataContract();
            }
        }

        #endregion
    }


    public class MyResourceHandler
    {
      
        public OperationResult Get()
        {
            return new OperationResult.OK(new MyResource {Name = "foobar"});
        }
    }

    public class MyResource
    {
        public string Name { get; set; }
    }

    public class Y
    {
    }

    public class X
    {
    }
}