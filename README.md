# Dapr 运用

* 前置条件
  * Docker
  * Win10

## Dapr 部署

本文将采用本地部署的方式。

### 安装 Dapr CLI

打开 Windows PowerShell 或  cmd ，运行以下命令以安装 `Dapr CLI`，并添加安装路径到系统环境变量中。

``` PowerShell
powershell -Command "iwr -useb https://raw.githubusercontent.com/dapr/cli/master/install/install.ps1 | iex"
```

 这里安装可能会失败。如果失败可以手动安装。

* 打开 Dapr [发布](https://github.com/dapr/cli/releases)页面下载 `dapr_windows_amd64.zip`
* 解压文件 zip 文件
* 把解压后的文件拷贝到 `C:\dapr` 中

### 安装 MySql

Docker 启动 Mysql

``` docker
docker run --name mysqltest -e MYSQL_ROOT_PASSWORD=123456 -d mysql
```

### 使用 Dapr CLI 安装 Darp runtime

在 Windows PowerShell 或 cmd 中使用命令 `dapr init` 以安装 Dapr。

![daprinit](https://raw.githubusercontent.com/SoMeDay-Zhang/DaprDemos/master/docs/images/daprinit.png)

同时可以在 Docker 中查看 Dapr 容器。

![daprContainers](https://raw.githubusercontent.com/SoMeDay-Zhang/DaprDemos/master/docs/images/daprcontainers.png)

至此，一个本地 Dapr 服务搭建完成。

## 使用 `Asp.Net Core` 搭建  ProductService 服务

ProductService 提供两个服务

* 获取所有产品集合
* 添加产品

1. 使用 `ASP.Net Core` 创建 ProductService ，具体参考源码

2. Dapr 启动 ProductService

    ``` cmd
    dapr run --app-id productService --app-port 5000 dotnet run
    ```

3. 获取所有产品集合，使用 curl 命令

    ``` curl
    curl -X GET http://localhost:5000/getlist
    ```

    或者

    ``` curl
    curl -X GET http://localhost:54680/v1.0/invoke/productService/method/getlist
    ```

4. 添加一个产品

   ``` curl
   curl -X POST https://localhost:5001/product -H "Content-Type: application/json" -d "{ \"id\": \"14a3611d-1561-455f-9c72-381eed2f6ee3\" }"
   ```

5. *重点*，通过 Dapr 添加一个产品，先看添加产品的代码

   ``` csharp
    /// <summary>
    /// 创建产品
    /// </summary>
    /// <param name="productCreate">产品创建模型</param>
    /// <returns></returns>
    [Topic("product")]
    [HttpPost("product")]
    public async Task<bool> CreateProduct(ProductCreate productCreate)
    {
        _productContext.Products.Add(new Product
        {
            ProductID = productCreate.ID
        });
        return await _productContext.SaveChangesAsync() == 1;
    }
   ```

   * 使用 Dapr cli 发布事件

        ``` cmd
         dapr invoke -a productService -m product -p "{\"id\":\"b1ccf14a-408a-428e-b0f0-06b97cbe4135\"}"
        ```

        输出为：

        ``` cmd
        true
        App invoked successfully
        ```

   * 使用 curl 命令直接请求 ProductService 地址

        ``` curl
        curl -X POST http://localhost:5000/product -H "Content-Type: application/json" -d "{ \"id\": \"14a3611d-1561-455f-9c72-381eed2f64e3\" }"
        ```

        输出为：

        ``` cmd
        true
        ```
  
   * 使用 curl 命令通过 Dapr runtime
  
       ``` curl
       curl -X POST http://localhost:54680/v1.0/invoke/productService/method/product -H "Content-Type: application/json" -d "{ \"id\": \"14a3611d-1561-455f-9c72-381eed2f54e3\" }"
       ```

      输出为：

      ``` cmd
      true
      ```

> **注意：**
>
> * Dapr 使用 App 端口号应与服务端口号相同，例如：`ASP.Net Core` 服务端口号为5000，则在使用 Dapr 托管应用程序时的端口号也应使用 5000

至此， ProductService 创建完成。

## 使用 Golang 创建 gRPC Server

1. 创建 Server

    ``` go
    package main

    import (
        "context"
        "fmt"
        "log"
        "net"

        "github.com/golang/protobuf/ptypes/any"
        "github.com/golang/protobuf/ptypes/empty"

        pb "github.com/dapr/go-sdk/daprclient"
        "google.golang.org/grpc"
    )

    // server is our user app
    type server struct {
    }

    func main() {
        // create listiner
        lis, err := net.Listen("tcp", ":4000")
        if err != nil {
            log.Fatalf("failed to listen: %v", err)
        }

        // create grpc server
        s := grpc.NewServer()
        pb.RegisterDaprClientServer(s, &server{})

        fmt.Println("Client starting...")

        // and start...
        if err := s.Serve(lis); err != nil {
            log.Fatalf("failed to serve: %v", err)
        }
    }

    // Sample method to invoke
    func (s *server) MyMethod() string {
        return "Hi there!"
    }

    // This method gets invoked when a remote service has called the app through Dapr
    // The payload carries a Method to identify the method, a set of metadata properties and an optional payload
    func (s *server) OnInvoke(ctx context.Context, in *pb.InvokeEnvelope) (*any.Any, error) {
        var response string

        fmt.Println(fmt.Sprintf("Got invoked with: %s", string(in.Data.Value)))

        switch in.Method {
        case "MyMethod":
            response = s.MyMethod()
        }
        return &any.Any{
            Value: []byte(response),
        }, nil
    }

    // Dapr will call this method to get the list of topics the app wants to subscribe to. In this example, we are telling Dapr
    // To subscribe to a topic named TopicA
    func (s *server) GetTopicSubscriptions(ctx context.Context, in *empty.Empty) (*pb.GetTopicSubscriptionsEnvelope, error) {
        return &pb.GetTopicSubscriptionsEnvelope{
            Topics: []string{"TopicA"},
        }, nil
    }

    // Dapper will call this method to get the list of bindings the app will get invoked by. In this example, we are telling Dapr
    // To invoke our app with a binding named storage
    func (s *server) GetBindingsSubscriptions(ctx context.Context, in *empty.Empty) (*pb.GetBindingsSubscriptionsEnvelope, error) {
        return &pb.GetBindingsSubscriptionsEnvelope{
            Bindings: []string{"storage"},
        }, nil
    }

    // This method gets invoked every time a new event is fired from a registerd binding. The message carries the binding name, a payload and optional metadata
    func (s *server) OnBindingEvent(ctx context.Context, in *pb.BindingEventEnvelope) (*pb.BindingResponseEnvelope, error) {
        fmt.Println("Invoked from binding")
        return &pb.BindingResponseEnvelope{}, nil
    }

    // This method is fired whenever a message has been published to a topic that has been subscribed. Dapr sends published messages in a CloudEvents 0.3 envelope.
    func (s *server) OnTopicEvent(ctx context.Context, in *pb.CloudEventEnvelope) (*empty.Empty, error) {
        fmt.Println("Topic message arrived")
        return &empty.Empty{}, nil
    }

    ```

2. 使用 Dapr 命令启动 StorageService

   ``` cmd
    dapr run --app-id client --protocol grpc --app-port 4000 go run main.go
   ```

> **注意：**
>
> * Dapr 使用 App 端口号应与服务端口号相同，使用 --protocal grpc 指定通讯协议为 grpc 。此外，OnInvoke 中的 switch 方法用于调用者路由。

## 使用 `ASP.NET Core` 创建 StorageService

1. 使用 NuGet 获取程序管理包控制台安装以下包
   * Dapr.AspNetCore
   * Dapr.Client.Grpc
   * Grpc.AspNetCore
   * Grpc.Net.Client

2. `Startup.cs` 文件中修改代码如下：
  
    ``` csharp
    /// <summary>
    /// This method gets called by the runtime. Use this method to add services to the container.
    /// </summary>
    /// <param name="services">Services.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddDapr();
        services.AddDbContextPool<StorageContext>(options => { options.UseMySql(Configuration.GetConnectionString("MysqlConnection")); });
    }
    ```

    ``` csharp
     /// <summary>
    /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    /// </summary>
    /// <param name="app">app.</param>
    /// <param name="env">env.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseCloudEvents();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapSubscribeHandler();
            endpoints.MapControllers();
        });
    }
    ```

3. 添加 `StorageController.cs` 文件，内容如下

    ``` csharp
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Dapr.Client.Grpc;
    using Google.Protobuf;
    using Grpc.Net.Client;
    using Microsoft.AspNetCore.Mvc;
    using StorageService.Api.Entities;

    namespace StorageService.Api.Controllers
    {
        [ApiController]
        public class StorageController : ControllerBase
        {
            private readonly StorageContext _storageContext;

            public StorageController(StorageContext storageContext)
            {
                _storageContext = storageContext;
            }

            /// <summary>
            /// 初始化仓库.
            /// </summary>
            /// <returns>是否成功.</returns>
            [HttpGet("InitialStorage")]
            public async Task<bool> InitialStorage()
            {
                string defaultPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? "54681";

                // Set correct switch to make insecure gRPC service calls. This switch must be set before creating the GrpcChannel.
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

                // Create Client
                string daprUri = $"http://127.0.0.1:{defaultPort}";
                GrpcChannel channel = GrpcChannel.ForAddress(daprUri);
                var client = new Dapr.Client.Grpc.Dapr.DaprClient(channel);
                Console.WriteLine(daprUri);

                InvokeServiceResponseEnvelope result = await client.InvokeServiceAsync(new InvokeServiceEnvelope
                {
                    Method = "MyMethod",
                    Id = "client",
                    Data = new Google.Protobuf.WellKnownTypes.Any
                    {
                        Value = ByteString.CopyFromUtf8("Hello ProductService")
                    }
                });
                Console.WriteLine("this is call result:" + result.Data.Value.ToStringUtf8());
                //var productResult = result.Data.Unpack<ProductList.V1.ProductList>();
                //Console.WriteLine("this is call result:" + productResult.Results.FirstOrDefault());
                return true;
            }

            /// <summary>
            /// 修改库存
            /// </summary>
            /// <param name="storage"></param>
            /// <returns></returns>
            [HttpPut("Reduce")]
            public bool Reduce(Storage storage)
            {
                Storage storageFromDb = _storageContext.Storage.FirstOrDefault(q => q.ProductID.Equals(storage.ProductID));
                if (storageFromDb == null)
                {
                    return false;
                }

                if (storageFromDb.Amount <= storage.Amount)
                {
                    return false;
                }

                storageFromDb.Amount -= storage.Amount;
                return true;
            }
        }
    }
    ```

4. 使用 Dapr cli 启用 StorageService 服务

    ``` powershell
    dapr run --app-id storageService --app-port 5003 dotnet run
    ```

5. 使用 curl 命令访问 StorageService InitialStorage 方法

    ``` curl
    curl -X GET http://localhost:56349/v1.0/invoke/storageService/method/InitialStorage
    ```

    输入

    ``` cmd
    true
    ```

    其中打印信息为：

    ``` cmd
    this is call result:Hi there!
    ```

> **注意：**
>
> * Dapr 使用 App 端口号应与服务端口号相同，例如：`ASP.Net Core` 服务端口号为5003，则在使用 Dapr 托管应用程序时的端口号也应使用 5003，在 Client.InvokeServiceAsync 中的 Id 指被调用方的 App-Id ,Method 指被调用方方法名称。参考 Go Server 中 OnInvoke 方法的 Switch 。

## 改造 ProductService 以提供 gRPC 服务

1. 从 NuGet 或程序包管理控制台安装 gRPC 服务必须的包

   * Grpc.AspNetCore

2. 配置 Http/2
   * gRPC 服务需要 Http/2 协议

        ``` csharp
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, 5001, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http2;
                        });
                    });
                    webBuilder.UseStartup<Startup>();
                });
        }
        ```

3. 新建了 product.proto 以定义 GRPC 服务，它需要完成的内容是返回所有产品集合，当然目前产品内容只有一个 ID

    * 定义产品 proto

        ``` proto
        syntax = "proto3";

        package productlist.v1;

        option csharp_namespace = "ProductList.V1";

        service ProductRPCService{
            rpc GetAllProducts(ProductListRequest) returns(ProductList);
        }

        message ProductListRequest{

        }

        message ProductList {
            repeated Product results = 1;
        }

        message Product {
            string ID=1;
        }
        ```

        说明
        * 定义产品列表 gRPC 服务，得益于宇宙第一 IDE Visual Studio ，只要添加 Grpc.Tools 包就可以自动生成 gRPC 所需的代码，这里不再需要手动去添加 Grpc.Tools ，官方提供的 Grpc.AspNetCore 中已经集成了
        * 定义了一个服务 ProductRPCService
        * 定义了一个函数 GetAllProducts
        * 定义了一个请求构造 ProductListRequest ，内容为空
        * 定义了一个请求返回构造 ProductList ，使用 repeated 表明返回数据是集合
        * 定义了一个数据集合中的一个对象 Product  
    * 添加 ProductListService 文件，内容如下

        ``` csharp
            public class ProductListService : ProductRPCService.ProductRPCServiceBase
            {
                private readonly ProductContext _productContext;

                public ProductListService(ProductContext productContext)
                {
                    _productContext = productContext;
                }

                public override async Task<ProductList.V1.ProductList> GetAllProducts(ProductListRequest request, ServerCallContext context)
                {
                    IList<Product> results = await _productContext.Products.ToListAsync();
                    var productList = new ProductList.V1.ProductList();
                    foreach (Product item in results)
                    {
                        productList.Results.Add(new ProductList.V1.Product
                        {
                            ID = item.ProductID.ToString()
                        });
                    }

                    return productList;
                }
            }
        ```

4. 在 Startup.cs 修改代码如下

    ``` csharp
    public void ConfigureServices(IServiceCollection services)
    {
        //启用 gRPC 服务
        services.AddGrpc();
        services.AddTransient<ProductListService>();
        ...
    }
    ```

    这里的 `services.AddTransient<ProductListService>()` ; 的原因是在 Dapr 中需要使用构造器注入，以完成 `GetAllProducts(...)` 函数的调用

    ``` csharp
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            ...

            //添加 gRPC 到路由管道中
            endpoints.MapGrpcService<DaprClientService>();
        });
    }
    ```

    这里添加的代码的含义分别是启用 gRPC 服务和添加 gRPC 路由。得益于 `ASP.NET Core` 中间件的优秀设计，`ASP.NET Core` 可同时支持 Http 服务。

5. 添加 daprclient.proto 文件以生成 Dapr Grpc 服务，daprclient.proto 内容如下

    ``` proto
    syntax = "proto3";

    package daprclient;

    import "google/protobuf/any.proto";
    import "google/protobuf/empty.proto";
    import "google/protobuf/duration.proto";

    option java_outer_classname = "DaprClientProtos";
    option java_package = "io.dapr";

    // User Code definitions
    service DaprClient {
    rpc OnInvoke (InvokeEnvelope) returns (google.protobuf.Any) {}
    rpc GetTopicSubscriptions(google.protobuf.Empty) returns (GetTopicSubscriptionsEnvelope) {}
    rpc GetBindingsSubscriptions(google.protobuf.Empty) returns (GetBindingsSubscriptionsEnvelope) {}
    rpc OnBindingEvent(BindingEventEnvelope) returns (BindingResponseEnvelope) {}
    rpc OnTopicEvent(CloudEventEnvelope) returns (google.protobuf.Empty) {}
    }

    message CloudEventEnvelope {
    string id = 1;
    string source = 2;
    string type = 3;
    string specVersion = 4;
    string dataContentType = 5;
    string topic = 6;
    google.protobuf.Any data = 7;
    }

    message BindingEventEnvelope {
        string name = 1;
        google.protobuf.Any data = 2;
        map<string,string> metadata = 3;
    }

    message BindingResponseEnvelope {
    google.protobuf.Any data = 1;
    repeated string to = 2;
    repeated State state = 3;
    string concurrency = 4;
    }

    message InvokeEnvelope {
        string method = 1;
        google.protobuf.Any data = 2;
        map<string,string> metadata = 3;
    }

    message GetTopicSubscriptionsEnvelope {
    repeated string topics = 1;
    }

    message GetBindingsSubscriptionsEnvelope {
    repeated string bindings = 1;
    }

    message State {
    string key = 1;
    google.protobuf.Any value = 2;
    string etag = 3;
    map<string,string> metadata = 4;
    StateOptions options = 5;
    }

    message StateOptions {
    string concurrency = 1;
    string consistency = 2;
    RetryPolicy retryPolicy = 3;
    }

    message RetryPolicy {
    int32 threshold = 1;
    string pattern = 2;
    google.protobuf.Duration interval = 3;
    }
    ```

    说明
    * 此文件为官方提供，Dapr 0.3 版本之前提供的已经生成好的代码，现在看源码可以看出已经改为提供 proto 文件了，这里我认为提供 proto 文件比较合理
    * 此文件定义了5个函数，此文主要讲的就是 `OnInvoke()` 函数
    * `OnInvoke()` 请求构造为 `InvokeEnvelope`
      * method 提供调用方法名称
      * data 请求数据
      * metadata 额外数据，此处使用键值对形式体现

6. 创建 DaprClientService.cs 文件，此文件用于终结点路由，内容为

    ``` csharp
    public class DaprClientService : DaprClient.DaprClientBase
    {
        private readonly ProductListService _productListService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductService" /> class.
        /// </summary>
        /// <param name="productListService"></param>
        public DaprClientService(ProductListService productListService)
        {
            _productListService = productListService;
        }

        public override async Task<Any> OnInvoke(InvokeEnvelope request, ServerCallContext context)
        {
            switch (request.Method)
            {
                case "GetAllProducts":
                    ProductListRequest productListRequest = ProductListRequest.Parser.ParseFrom(request.Data.Value);
                    ProductList.V1.ProductList productsList = await _productListService.GetAllProducts(productListRequest, context);
                    return Any.Pack(productsList);
            }

            return null;
        }
    }
    ```

    说明
    * 使用构造器注入已定义好的 `ProductListService`
    * `InvokeEnvelope` 中的 `Method` 用于路由数据
    * 使用 `ProductListRequest.Parser.ParseFrom` 转换请求构造
    * 使用 `Any.Pack()` 打包需要返回的数据

7. 运行 productService

    ``` cmd
    dapr run --app-id productService --app-port 5001 --protocol grpc dotnet run
    ```

>**小结**
>至此，ProductService 服务完成。此时 ProductService.Api.csproj Protobuf 内容为
>
> ``` cmd
> <ItemGroup>
>   <Protobuf Include="Protos\daprclient.proto" GrpcServices="Server" />
>  <Protobuf Include="Protos\productList.proto" GrpcServices="Server" />
>  </ItemGroup>
> ```

## 改造 StorageService 服务以完成 Dapr GRPC 服务调用

1. 添加 productList.proto 文件，内容同 ProductService 中的 productList.proto

2. 添加 dapr.proto 文件，此文件也为官方提供，内容为

    ``` proto
    syntax = "proto3";

    package dapr;

    import "google/protobuf/any.proto";
    import "google/protobuf/empty.proto";
    import "google/protobuf/duration.proto";

    option java_outer_classname = "DaprProtos";
    option java_package = "io.dapr";

    option csharp_namespace = "Dapr.Client.Grpc";


    // Dapr definitions
    service Dapr {
    rpc PublishEvent(PublishEventEnvelope) returns (google.protobuf.Empty) {}
    rpc InvokeService(InvokeServiceEnvelope) returns (InvokeServiceResponseEnvelope) {}
    rpc InvokeBinding(InvokeBindingEnvelope) returns (google.protobuf.Empty) {}
    rpc GetState(GetStateEnvelope) returns (GetStateResponseEnvelope) {}
    rpc SaveState(SaveStateEnvelope) returns (google.protobuf.Empty) {}
    rpc DeleteState(DeleteStateEnvelope) returns (google.protobuf.Empty) {}
    }

    message InvokeServiceResponseEnvelope {
    google.protobuf.Any data = 1;
    map<string,string> metadata = 2;
    }

    message DeleteStateEnvelope {
    string key = 1;
    string etag = 2;
    StateOptions options = 3;
    }

    message SaveStateEnvelope {
    repeated StateRequest requests = 1;
    }

    message GetStateEnvelope {
        string key = 1;
        string consistency = 2;
    }

    message GetStateResponseEnvelope {
    google.protobuf.Any data = 1;
    string etag = 2;
    }

    message InvokeBindingEnvelope {
    string name = 1;
    google.protobuf.Any data = 2;
    map<string,string> metadata = 3;
    }

    message InvokeServiceEnvelope {
    string id = 1;
    string method = 2;
    google.protobuf.Any data = 3;
    map<string,string> metadata = 4;
    }

    message PublishEventEnvelope {
        string topic = 1;
        google.protobuf.Any data = 2;
    }

    message State {
    string key = 1;
    google.protobuf.Any value = 2;
    string etag = 3;
    map<string,string> metadata = 4;
    StateOptions options = 5;
    }

    message StateOptions {
    string concurrency = 1;
    string consistency = 2;
    RetryPolicy retryPolicy = 3;
    }

    message RetryPolicy {
    int32 threshold = 1;
    string pattern = 2;
    google.protobuf.Duration interval = 3;
    }

    message StateRequest {
    string key = 1;
    google.protobuf.Any value = 2;
    string etag = 3;
    map<string,string> metadata = 4;
    StateRequestOptions options = 5;
    }

    message StateRequestOptions {
    string concurrency = 1;
    string consistency = 2;
    StateRetryPolicy retryPolicy = 3;
    }

    message StateRetryPolicy {
    int32 threshold = 1;
    string pattern = 2;
    google.protobuf.Duration interval = 3;
    }
    ```

    说明
    * 此文件提供6个 GRPC 服务，此文介绍的函数为 `InvokeService()`
      * 请求构造为 InvokeServiceEnvelope
        * id 请求的服务的 --app-id ，比如 productService
        * method 请求的方法
        * data 请求函数的签名
        * metadata 元数据键值对

3. 修改 StorageController 中的 `InitialStorage()` 函数为

    ``` csharp
    /// <summary>
    /// 初始化仓库.
    /// </summary>
    /// <returns>是否成功.</returns>
    [HttpGet("InitialStorage")]
    public async Task<bool> InitialStorage()
    {
        string defaultPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? "5001";

        // Set correct switch to make insecure gRPC service calls. This switch must be set before creating the GrpcChannel.
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        // Create Client
        string daprUri = $"http://127.0.0.1:{defaultPort}";
        GrpcChannel channel = GrpcChannel.ForAddress(daprUri);
        var client = new Dapr.Client.Grpc.Dapr.DaprClient(channel);

        InvokeServiceResponseEnvelope result = await client.InvokeServiceAsync(new InvokeServiceEnvelope
        {
            Method = "GetAllProducts",
            Id = "productService",
            Data = Any.Pack(new ProductListRequest())
        });
        ProductList.V1.ProductList productResult = ProductList.V1.ProductList.Parser.ParseFrom(result.Data.Value);

        var random = new Random();

        foreach (Product item in productResult.Results)
        {
            _storageContext.Storage.Add(new Storage
            {
                ProductID = Guid.Parse(item.ID),
                Amount = random.Next(1, 1000)
            });
        }

        await _storageContext.SaveChangesAsync();
        return true;
    }
    ```

4. 启动 StorageService

    ``` cmd
    dapr run --app-id storageService --app-port 5003 dotnet run
    ```

5. 使用 Postman 请求 StorageService 的 InitialStorage
    ![InitialStorage](https://raw.githubusercontent.com/SoMeDay-Zhang/DaprDemos/master/docs/images/daprInitialStorage.png)
6. 使用 MySql Workbench 查看结果
    ![InitialStorageResult](https://raw.githubusercontent.com/SoMeDay-Zhang/DaprDemos/master/docs/images/daprInitialStorageResult.png)

>**小结**
>至此，以 Dapr 框架使用 GRPC 客户端在 StorageService 中完成了对 ProductService 服务的调用。

## JAVA GRPC 服务与调用

### 安装协议编译器

1. 下载对应的版本[编译器](https://github.com/protocolbuffers/protobuf/releases/tag/v3.11.2)，并把路径加入到环境变量中，执行以下命令生成代码

    ``` cmd
    protoc -I=$SRC_DIR --java_out=$DST_DIR $SRC_DIR/addressbook.proto
    ```

    `-I` 表示源码所在文件夹位置，`--java_out` 表示输出路径，空格后表示具体的 proto 文件位置，以下为示例命令

   ``` cmd
    protoc -I=C:\Users\JR\DaprDemos\java\examples\src\main\protos\examples --java_out=C:\Users\JR\DaprDemos\java\examples\src\main\java  C:\Users\JR\DaprDemos\java\examples\src\main\protos\examples\helloworld.proto
   ```

2. 启动 Dapr gRPC 服务端

    ``` cmd
    dapr run --app-id hellogrpc --app-port 5000 --protocol grpc -- mvn exec:java -pl=examples -Dexec.mainClass=server.HelloWorldService -Dexec.args="-p 5000"
    ```

    服务端主要实现说明
    * 通过 Java SDK（实际此 SDK 可通过 protoc 自己生成，完成没有必要引用官方给的 SDK） 实现 dapr 对 gRPC 的通讯封装
    * 服务端 proto 文件为 daprclient.proto ，鉴于语言之间的不同，名字看上去有点奇怪。（比如：以 client 为后缀，实际是服务端）
    * 如果使用 Java SDK 则需要 Override `onInvoke()` 函数，该函数为 Dapr gRPC 调用封装。该函数提供两个签名 `InvokeEnvelope` 和 `StreamObserver<Any>`
      * `InvokeEnvelope` 用于解析 gRPC 请求函数
      * `StreamObserver<Any>` 用于疯转 gRPC 应答
    * helloworld.proto
      * 定义了一个 gRPC 函数 `Say`
      * 定义了函数签名 `SayRequest`
      * 定义了函数返回类型 `SayResponse`
      * 根据步骤1提供的 cmd 命令生成代码以在 `onInvoke` 函数中调用

3. 启动 Dapr gRPC 客户端

    ``` cmd
    dapr run --protocol grpc --grpc-port 50001 -- mvn exec:java -pl=examples -Dexec.mainClass=client.HelloWorldClient -Dexec.args="-p 50001 'message one' 'message two'"
    ```

    客户端主要实现说明
    * 客户端 proto 文件为 dapr.proto
    * 使用生成代码调用 `InvokeServiceEnvelope()` 函数
      * setId 设置该函数需要调用的服务 Id ，该 Id 指在使用 Dapr 启动实例时 --app-id 指定的名称（例如步骤2中的 hellogrpc）
      * setData 设置调用函数的签名
      * setMethod 设置调用函数名称

4. gRPC 服务端收到消息

    输出为：

    ``` cmd
     Server: message one
     Server: message two
    ```

至此， Java 客户端服务端通过 Dapr 完成 gRPC 通讯。

## K8S 集成 Dapr

前置条件

* [搭建 K8S 本地集群](https://github.com/dapr/docs/blob/master/getting-started/cluster/setup-minikube.md)

1. 使用命令查看 K8S 本地集群 Dashboard

    ``` cmd
    minikube dashboard
    ```

    此命令可以获取到 Dashboard 的代理地址，复制地址到浏览器中以进行查阅。

2. 查看 Dapr Pods
    通过输入 `kubectl get ns` 获取到本地集群中的所有命名空间。

    ``` cmd
    kubectl get ns
    ```

    输出为

    ``` cmd
    NAME                   STATUS   AGE
    dapr-system            Active   18h
    default                Active   18h
    kube-node-lease        Active   18h
    kube-public            Active   18h
    kube-system            Active   18h
    kubernetes-dashboard   Active   18h
    ```

    此时可以看到 Dapr 的命名空间为 dapr-system 。查看 dapr-system 下的所有 pods 。

    ``` cmd
    kubectl get pods -n dapr-system
    ```

    输出为

    ``` cmd
    NAME                                     READY   STATUS    RESTARTS   AGE
    dapr-operator-7c6799878d-sp455           1/1     Running   0          18h
    dapr-placement-76c99b79bb-plgkl          1/1     Running   0          18h
    dapr-sidecar-injector-84c5578f8d-bfsls   1/1     Running   0          18h
    ```

3. 搭建私有 Docker Repository

    * 打开 DockerDesktop -> Settings -> Deamon ，在 Registry mirrors 中添加 `http://hub-mirror.c.163.com` ,点击 Apply ， Docker Desktop 将自动重启以应用更改。这里使用了网易的镜像源
    * 打开 cmd 运行 `docker pull registry:latest` 以获取最近的 registry 镜像
    * 启动 registry 镜像以搭建本地镜像仓库

      ``` cmd
      docker run -d -p 8900:5000 --restart always --name registry registry:latest
      ```

    * 构建镜像 - buid

      ``` cmd
      docker build -f dockerfile文件所在位置绝对路径 --force-rm -t 192.168.1.243:8900/productserviceapi:dev  "c:\users\jr\daprdemos\dotnetcore"
      ```

      打包成功后，输入 `docker images` 以查看生成的镜像

      ``` cmd
      REPOSITORY                             TAG                 IMAGE ID            CREATED             SIZE
      192.168.1.243:8999/productserviceapi   dev                 3c3b4b41a4e3        14 minutes ago      232MB
      ```

    * 推送镜像到本地仓库 - push

      ``` cmd
      docker push 192.168.1.243:8999/productserviceapi:dev
      ```

      输出为

      ``` cmd
      Get https://192.168.1.243:8999/v2/: http: server gave HTTP response to HTTPS client
      ```

      打开 DockerDesktop -> Settings -> Deamon -> Insecure registries 中输入`192.168.1.243:8999` ，点击 Apply ， Docker Desktop 将自动重启以应用更改

      再次运行 Push 命令，输出为

      ``` cmd
      The push refers to repository [192.168.1.243:8999/productserviceapi]
      9a8e684f88ea: Pushed
      3044a592a506: Pushed
      62b3f719c3a6: Pushed
      52d5ea296228: Pushed
      239bf536471e: Pushed
      cad0d4e88a35: Pushed
      831c5620387f: Pushed
      dev: digest: sha256:5f3f79c6a45cf073e05f5426c858f5ce63cbc8e34639add81eed23b80fe70286 size: 1792
      ```

## Dapr Pub/Sub 集成 RabbitMQ

1. 搭建 RabbitMQ

    * Docker 搭建 RabbitMQ 服务

        ``` cmd
        docker run -d --hostname my-rabbit --name some-rabbit -p 15672:15672 rabbitmq:3-management
        ```

    * 创建 messagebus.yaml

        ``` yaml
        apiVersion: dapr.io/v1alpha1
        kind: Component
        metadata:
        name:messagebus
        spec:
        type: pubsub.rabbitmq
        metadata:
        - name: host
            value: "localhost:5672" # Required. Example: "rabbitmq.default.svc.cluster.local:5672"
        - name: consumerID
            value: "61415901178272324029" # Required. Any unique ID. Example: "myConsumerID"
        - name: durable
            value: "true" # Optional. Default: "false"
        - name: deletedWhenUnused
            value: "false" # Optional. Default: "false"
        - name: autoAck
            value: "false" # Optional. Default: "false"
        - name: deliveryMode
            value: "2" # Optional. Default: "0". Values between 0 - 2.
        - name: requeueInFailure
            value: "true" # Optional. Default: "false".
        ```
