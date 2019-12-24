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
        request.setCustomerID("4f6ec095-35b4-40f0-8cf5-a544712434e4");
        CreateOrderProtos.GetOrderListResponse response=service.getOrderList(request.build());
        assert response.getOrdersCount()==1;
    }

    @Test
    public void retrieveOrder() {
        GrpcHelloWorldDaprService service=new GrpcHelloWorldDaprService();
        CreateOrderProtos.RetrieveOrderRequest.Builder request=CreateOrderProtos.RetrieveOrderRequest.newBuilder();
        request.setOrderID("45895e1b-79f3-48b8-9f99-8347a4f50a5e");
        CreateOrderProtos.RetrieveOrderResponse response=service.retrieveOrder(request.build());
        assert response.getOrder().getAmount()==2;
    }
}