package server.dapr.grpc.services;

import generate.protos.CreateOrderProtos;
import org.junit.Test;

import java.util.UUID;

public class GrpcHelloWorldDaprServiceTest {

    @Test
    public void createOrder() {
        GrpcHelloWorldDaprService service=new GrpcHelloWorldDaprService();
        CreateOrderProtos.CreateOrderRequest.Builder request=CreateOrderProtos.CreateOrderRequest.newBuilder();
        request.setAmount(2);
        request.setCustomerID(UUID.randomUUID().toString());
        request.setProductID(UUID.randomUUID().toString());

        CreateOrderProtos.CreateOrderResponse response = service.createOrder(request.build());
        assert (response.getSucceed());
    }

    @Test
    public void getOrderList() {
        GrpcHelloWorldDaprService service=new GrpcHelloWorldDaprService();
        CreateOrderProtos.GetOrderListRequest.Builder request=CreateOrderProtos.GetOrderListRequest.newBuilder();
        request.setCustomerID("0d158a88-73de-42e5-87c7-fdbc00bdc5f9");
        CreateOrderProtos.GetOrderListResponse response=service.getOrderList(request.build());
        assert response.getOrdersCount()>1;
    }

    @Test
    public void retrieveOrder() {
        GrpcHelloWorldDaprService service=new GrpcHelloWorldDaprService();
        CreateOrderProtos.RetrieveOrderRequest.Builder request=CreateOrderProtos.RetrieveOrderRequest.newBuilder();
        request.setOrderID("0d51d0e3-1fa7-4241-83ea-ddd2734c2595");
        CreateOrderProtos.RetrieveOrderResponse response=service.retrieveOrder(request.build());
        assert response.getOrder().getAmount()==20;
    }
}