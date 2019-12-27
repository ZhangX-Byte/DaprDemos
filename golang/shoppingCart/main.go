package main

import (
	"context"
	"fmt"
	"os"

	pb "github.com/dapr/go-sdk/dapr"
	"github.com/golang/protobuf/proto"
	"github.com/golang/protobuf/ptypes"
	"google.golang.org/grpc"

	"github.com/golang/protobuf/ptypes/any"

	"daprdemos/golang/shoppingCart/protos/customer_v1"
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

	req := &customer_v1.IdRequest{
		Id: "1e88e584-dcbd-44f6-9960-53c2ad687399",
	}
	data, err := ptypes.MarshalAny(req)
	if err != nil {
		fmt.Println(err)
	} else {
		fmt.Println(data)
	}

	// Invoke a method called MyMethod on another Dapr enabled service with id client
	resp, err := client.InvokeService(context.Background(), &pb.InvokeServiceEnvelope{
		Id:     "CustomerService",
		Data:   data,
		Method: "GetCustomerById",
	})
	if err != nil {
		fmt.Println(err)
	} else {
		result := &customer_v1.Customer{}

		if err := proto.Unmarshal(resp.Data.Value, result); err == nil {
			fmt.Println(result)
			fmt.Println(result.Name)
			client.PublishEvent(context.Background(), &pb.PublishEventEnvelope{
				Topic: "Test",
				Data: &any.Any{
					Value: []byte(result.Name),
				},
			})
		} else {
			fmt.Println(err)
		}
	}
}
