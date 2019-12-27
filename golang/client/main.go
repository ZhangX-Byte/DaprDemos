package main

import (
	"context"
	"fmt"
	"os"

	"daprdemos/golang/client/protos/productlist_v1"
	"daprdemos/golang/client/protos/shoppingCart"

	pb "github.com/dapr/go-sdk/dapr"
	"github.com/golang/protobuf/proto"
	"github.com/golang/protobuf/ptypes"
	"github.com/golang/protobuf/ptypes/any"
	"google.golang.org/grpc"
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
	productList := getAllProducts(client)

	//加入产品到购物车
	addProduct(client, productList.Results[0].ID)

	//获取购物车
	getShoppingCart(client)
}

func getAllProducts(client pb.DaprClient) *productlist_v1.ProductList {
	fmt.Println("获取产品列表")
	response, err := client.InvokeService(context.Background(), &pb.InvokeServiceEnvelope{
		Id:     "productService",
		Data:   &any.Any{},
		Method: "GetAllProducts",
	})
	if err != nil {
		fmt.Println(err)
		return nil
	}

	productList := &productlist_v1.ProductList{}
	if err := proto.Unmarshal(response.Data.Value, productList); err != nil {
		fmt.Println(err)
		return nil
	}
	for _, product := range productList.Results {
		fmt.Println(product.ID)
	}

	return productList
}

func addProduct(client pb.DaprClient, productID string) {
	fmt.Println("加入产品到购物车")
	addProductRequest := &shoppingCart.AddProductRequest{
		ProductID: productID,
	}
	data, err := ptypes.MarshalAny(addProductRequest)
	if err != nil {
		fmt.Println(err)
	} else {
		fmt.Println(data)
	}

	response, err := client.InvokeService(context.Background(), &pb.InvokeServiceEnvelope{
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
}

func getShoppingCart(client pb.DaprClient) {
	fmt.Println("获取购物车")
	response, err := client.InvokeService(context.Background(), &pb.InvokeServiceEnvelope{
		Id:     "shoppingCartService",
		Data:   &any.Any{},
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
