package main

import (
	"context"
	"fmt"
	"os"

	pb "github.com/dapr/go-sdk/dapr"
	"github.com/golang/protobuf/proto"
	"github.com/golang/protobuf/ptypes"
	"google.golang.org/grpc"

	"daprdemos/golang/client/protos/productlist_v1"
	"daprdemos/golang/client/protos/shoppingCart"
)

func main() {
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

	//获取产品列表
	fmt.Println("获取产品列表")
	productListRequest := &productlist_v1.ProductListRequest{}
	data, err := ptypes.MarshalAny(productListRequest)
	if err != nil {
		fmt.Println(err)
	} else {
		fmt.Println(data)
	}
	response, err := client.InvokeService(context.Background(), &pb.InvokeServiceEnvelope{
		Id:     "productService",
		Data:   data,
		Method: "GetAllProducts",
	})
	if err != nil {
		fmt.Println(err)
		return
	}

	productList := &productlist_v1.ProductList{}
	if err := proto.Unmarshal(response.Data.Value, productList); err != nil {
		fmt.Println(err)
		return
	}
	for _, product := range productList.Results {
		fmt.Println(product.ID)
	}

	//加入产品到购物车
	fmt.Println("加入产品到购物车")
	addProductRequest := &shoppingCart.AddProductRequest{
		ProductID: productList.Results[0].ID,
	}
	data, err = ptypes.MarshalAny(addProductRequest)
	if err != nil {
		fmt.Println(err)
	} else {
		fmt.Println(data)
	}

	response, err = client.InvokeService(context.Background(), &pb.InvokeServiceEnvelope{
		Id:     "shoppingCartService",
		Data:   data,
		Method: "AddProduct",
	})
	if err != nil {
		fmt.Println(err)
		return
	}

	addProductResponse := &shoppingCart.AddProductResponse{}
	if err := proto.Unmarshal(response.Data.Value, addProductResponse); err != nil {
		fmt.Println(err)
		return
	}
	fmt.Println(addProductResponse.Succeed)

	//获取购物车
	fmt.Println("获取购物车")
	response, err = client.InvokeService(context.Background(), &pb.InvokeServiceEnvelope{
		Id:     "shoppingCartService",
		Data:   data,
		Method: "GetShoppingCart",
	})
	if err != nil {
		fmt.Println(err)
		return
	}

	getShoppingCartResponse := &shoppingCart.GetShoppingCartResponse{}
	if err := proto.Unmarshal(response.Data.Value, getShoppingCartResponse); err != nil {
		fmt.Println(err)
		return
	}
	fmt.Println(getShoppingCartResponse.ProductID)
}
