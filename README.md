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

![daprinit](https://raw.githubusercontent.com/SoMeDay-Zhang/daprintro/master/images/daprinit.png?token=ABQQC4NMBWSDZRAMTF3SCO256M55Q)

同时可以在 Docker 中查看 Dapr 容器。

![daprContainers](https://raw.githubusercontent.com/SoMeDay-Zhang/daprintro/master/images/daprcontainers.png?token=ABQQC4I23JOLGOU4ZGW4HHC56M6AG)

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

2. 在 Startup.cs 修改代码如下

    ``` csharp
    public void ConfigureServices(IServiceCollection services)
    {
        //启用 gRPC 服务
        services.AddGrpc();
        ...
    }
    ```

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

3. 配置 Http/2
   * gRPC 服务需要 Http/2 协议。

        ``` csharp
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, 5001, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http2;
                            listenOptions.UseHttps("<path to .pfx file>", 
                                "<certificate password>");
                        });
                    });
                    webBuilder.UseStartup<Startup>();
                });
        ```

   * 使用 [dev-certs](https://docs.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-2.1?view=aspnetcore-3.0#https) 以生成证书

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

3. 启动 Dapr gRPC 客户端

    ``` cmd
    dapr run --protocol grpc --grpc-port 50001 -- mvn exec:java -pl=examples -Dexec.mainClass=client.HelloWorldClient -Dexec.args="-p 50001 'message one' 'message two'"
    ```

4. gRPC 服务端收到消息

    输出为：

    ``` cmd
     Server: message one
     Server: message two
    ```

至此， Java 客户端服务端通过 Dapr 完成 gRPC 通讯。

[源码地址](https://github.com/SoMeDay-Zhang/DaprDemos)
