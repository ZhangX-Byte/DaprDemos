package main

import (
	"context"
	"fmt"
	"os"

	pb "github.com/dapr/go-sdk/dapr"
	"github.com/golang/protobuf/proto"
	"github.com/golang/protobuf/ptypes"
	"google.golang.org/grpc"

	"daprdemos/golang/shoppingCartForJava/protos/daprexamples"
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

	createOrderRequest := &daprexamples.CreateOrderRequest{
		ProductID:  "1",
		Amount:     20,
		CustomerID: "1",
	}
	createOrderRequestData, err := ptypes.MarshalAny(createOrderRequest)
	if err != nil {
		fmt.Println(createOrderRequestData)
	} else {
		fmt.Println(createOrderRequestData)
	}

	// Invoke a method called MyMethod on another Dapr enabled service with id client
	response, err := client.InvokeService(context.Background(), &pb.InvokeServiceEnvelope{
		Id:     "OrderService",
		Data:   createOrderRequestData,
		Method: "CreateOrder",
	})
	if err != nil {
		fmt.Println(err)
		return
	}

	createOrderResponse := &daprexamples.CreateOrderResponse{}

	if err := proto.Unmarshal(response.Data.Value, createOrderResponse); err != nil {
		fmt.Println(err)
		return
	}
	fmt.Println(createOrderResponse.Succeed)

	if !createOrderResponse.Succeed {
		//下单失败
		return
	}

	storageReduceData := &daprexamples.StorageReduceData{
		ProductID: createOrderRequest.ProductID,
		Amount:    createOrderRequest.Amount,
	}
	storageReduceDataData, err := ptypes.MarshalAny(storageReduceData)
	if err != nil {
		client.PublishEvent(context.Background(), &pb.PublishEventEnvelope{
			Topic: "Storage.Reduce",
			Data:  storageReduceDataData,
		})
	}
}
