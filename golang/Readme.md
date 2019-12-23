# Dapr使用GoLnag

## golang调用.net

本例子用来演示调用已有的.net ProductService的gRPC服务的GetAllProducts方法

1.启动.net ProductService的gRPC服务

```
dapr run --app-id productService --app-port 5001 --protocol grpc dotnet run
```

2.启动golang的客户端进行调用

golang的例子代码在https://github.com/SoMeDay-Zhang/DaprDemos/tree/master/golang/client 下

在client目录下运行命令

```
dapr run --app-id client go run ./
```

当可以看到以下输出证明调用成功

```
?[0m?[94;1m== APP == 0025844d-479c-4b1e-8444-5bcd48934523
?[0m?[94;1m== APP == 018f1680-1dd0-4a6a-adac-64377ec55e3d
?[0m?[94;1m== APP == 039922d6-970e-4e2f-b6f9-15cd2a4d1641
?[0m?[94;1m== APP == 06c5dc43-fb7f-4097-85ba-a1fd5e98dcf8
?[0m?[94;1m== APP == 0882c129-0d6f-4c74-b4d1-93fe8bbd81f2
...
```

3.golang开发

先试用proto文件生成pb.go文件，复制.net项目里的 productList.proto 到 client/proros/productlist_v1/ 文件夹下，在该目录下执行命令

```
protoc --go_out=plugins=grpc:. *.proto
```

正常情况是会在该目录下生成 productList.pb.go 文件   
如果没有生成，则需要安装这个： https://github.com/golang/protobuf 

4.具体代码详解

初始化daprClient

```
	// Get the Dapr port and create a connection
	daprPort := os.Getenv("DAPR_GRPC_PORT")
	daprAddress := fmt.Sprintf("localhost:%s", daprPort)
	conn, err := grpc.Dial(daprAddress, grpc.WithInsecure())
	if err != nil {
		fmt.Println(err)
		return
	}
	defer conn.Close()

	// Create the client
	client := pb.NewDaprClient(conn)
```

初始化请求GetAllProducts方法的调用参数

```
req := &productlist_v1.ProductListRequest{}
	any, err := ptypes.MarshalAny(req)
	if err != nil {
		fmt.Println(err)
	} else {
		fmt.Println(any)
	}
```

调用微服务

```
	// Invoke a method called MyMethod on another Dapr enabled service with id client
	resp, err := client.InvokeService(context.Background(), &pb.InvokeServiceEnvelope{
		Id:     "productService",
		Data:   any,
		Method: "GetAllProducts",
	})
```

解析返回数据

```
    result := &productlist_v1.ProductList{}

		if err := proto.Unmarshal(resp.Data.Value, result); err == nil {
			for _, product := range result.Results {
				fmt.Println(product.ID)
			}
		} else {
			fmt.Println(err)
		}
```
