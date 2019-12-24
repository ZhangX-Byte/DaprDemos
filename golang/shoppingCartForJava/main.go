package main

import (
	"context"
	"fmt"
	"os"

	pb "github.com/dapr/go-sdk/dapr"
	"github.com/golang/protobuf/proto"
	"github.com/golang/protobuf/ptypes"
	"google.golang.org/grpc"

	"daprdemos/golang/shoppingCart/protos/daprexamples"
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

	req := &daprexamples.CreateOrderRequest{
		ProductID:  "1",
		Amount:     20,
		CustomerID: "1",
	}
	any, err := ptypes.MarshalAny(req)
	if err != nil {
		fmt.Println(err)
	} else {
		fmt.Println(any)
	}

	// Invoke a method called MyMethod on another Dapr enabled service with id client
	resp, err := client.InvokeService(context.Background(), &pb.InvokeServiceEnvelope{
		Id:     "OrderService",
		Data:   any,
		Method: "CreateOrder",
	})
	if err != nil {
		fmt.Println(err)
	} else {
		result := &daprexamples.CreateOrderResponse{}

		if err := proto.Unmarshal(resp.Data.Value, result); err == nil {
			fmt.Println(result.Succeed)
		} else {
			fmt.Println(err)
		}
	}
}
