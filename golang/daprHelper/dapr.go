package daprHelper

import (
	"context"
	"fmt"
	"os"

	pb "github.com/dapr/go-sdk/dapr"
	"google.golang.org/grpc"
)

var Client pb.DaprClient

func init() {
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
	Client = pb.NewDaprClient(conn)
}

type invokeFunc func(context.Context, *interface{}) error

type InvokeServiceEnvelope struct {
	InvokeFunc invokeFunc
	Request    *interface{}
	Id         string
	Method     string
}

func InvokeService(in *InvokeServiceEnvelope, response *interface{}) error {

	invokeServiceEnvelope := &pb.InvokeServiceEnvelope{}
	invokeServiceResponseEnvelope, err := Client.InvokeService(context.Background(), invokeServiceEnvelope)
	if err != nil {
		return err
	}

	invokeServiceResponseEnvelope

	return nil
}
