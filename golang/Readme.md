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


## golang调用golang

服务端代码在customer下   
客户端代码在shoppingCart下  
客户端代码与 golang调用.net 基本一致  

### 服务端代码详解

1.新建 customer.proto 文件 定义传输规范

```
syntax = "proto3";

package customer.v1;

service CustomerService {
    rpc GetCustomerById(IdRequest) returns (Customer);
}

message IdRequest {
    string id = 1;
}

message Customer {
    string id = 1;
    string name = 2;
}
```

然后生成 customer.pb.go 文件

```
protoc --go_out=plugins=grpc:. *.proto
```

2.新建 customerService.go 文件，用来进行服务端处理

```
package service

import (
	pb "daprdemos/golang/customer/protos/customer_v1"
)

type CustomerService struct {
}

func (s *CustomerService) GetCustomerById(req *pb.IdRequest) pb.Customer {
	return pb.Customer{
		Id:   req.Id,
		Name: "小红",
	}
}

```

3.新建 main.go 入口文件

监听grpc服务并注册DaprClientServer

```
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
```

实现DaprClientServer

```
type server struct {
}

func (s *server) OnInvoke(ctx context.Context, in *pb.InvokeEnvelope) (*any.Any, error) {
	fmt.Println(fmt.Sprintf("Got invoked with: %s", string(in.Data.Value)))

	switch in.Method {
	case "GetCustomerById":
		input := &customer_v1.IdRequest{}

		customerService := &service.CustomerService{}

		proto.Unmarshal(in.Data.Value, input)
		resp := customerService.GetCustomerById(input)
		any, err := ptypes.MarshalAny(&resp)
		return any, err
	}
	return &any.Any{}, nil
}
```

### golang使用orm

详细文档参见 https://gorm.io/zh_CN/docs/index.html 

1.安装gorm

```
go get -u github.com/jinzhu/gorm
```

2.新建模型

```
package models

import (
	"github.com/google/uuid"
	"github.com/jinzhu/gorm"
)

type Customer struct {
	ID   uuid.UUID `gorm:"primary_key;type:varchar(36)"`
	Name string
}

func (customer *Customer) BeforeCreate(scope *gorm.Scope) error {
	if idField, ok := scope.FieldByName("ID"); ok {
		if idField.IsBlank {
			idField.Set(uuid.New())
		}
	}
	return nil
}

```

BeforeCreate是一个钩子函数，在创建对象前调用


3.新建数据库迁移

自动迁移 只会 创建表、缺失的列、缺失的索引， 不会 更改现有列的类型或删除未使用的列

```
package db

import "daprdemos/golang/customer/models"

func init() {
	DB.AutoMigrate(&models.Customer{})
}
```

4.连接数据库

连接数据库的配置可以自己处理，当前代码使用了github.com/jinzhu/configor

```
package db

import (
	"daprdemos/golang/customer/config"
	"fmt"

	"github.com/jinzhu/gorm"

	// 初始化mysql
	_ "github.com/jinzhu/gorm/dialects/mysql"
)

// DB Global DB connection
var DB *gorm.DB

func init() {
	var err error

	dbConfig := config.Config.DB
	DB, err = gorm.Open("mysql", fmt.Sprintf("%v:%v@tcp(%v:%v)/%v?parseTime=True&loc=Local", dbConfig.User, dbConfig.Password, dbConfig.Host, dbConfig.Port, dbConfig.Name))

	if err != nil {
		fmt.Println(err)
		panic(err)
	}
}
```

5.播种

引用第4步的DB即可直接操作数据库

```
package main

import (
	"daprdemos/golang/customer/config/db"
	"daprdemos/golang/customer/models"
	"fmt"
	"strconv"

	"github.com/google/uuid"
)

func main() {
	fmt.Println("start ...")
	var count int
	db.DB.Model(&models.Customer{}).Count(&count)
	if count == 0 {
		for index := 0; index < 100; index++ {
			var guid uuid.UUID
			if index == 0 {
				guid, _ = uuid.Parse("1e88e584-dcbd-44f6-9960-53c2ad687399")
			}
			db.DB.Create(&models.Customer{
				ID:   guid,
				Name: "小红" + strconv.Itoa(index),
			})
		}
	}
	fmt.Println("done")
}
```

首先创建一个新的数据库，然后运行main，即可创建表结构，并将初始化数据播种到数据库

6.改造原有customerService.go，使用数据库读取数据

```
package service

import (
	"daprdemos/golang/customer/config/db"
	"daprdemos/golang/customer/models"
	pb "daprdemos/golang/customer/protos/customer_v1"
)

type CustomerService struct {
}

func (s *CustomerService) GetCustomerById(req *pb.IdRequest) pb.Customer {
	var customer models.Customer
	db.DB.First(&customer, "id = ?", req.Id)
	return pb.Customer{
		Id:   customer.ID.String(),
		Name: customer.Name,
	}
}
```
