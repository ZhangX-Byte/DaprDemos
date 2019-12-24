package server.dapr.grpc.services;

import com.google.protobuf.Any;
import com.google.protobuf.InvalidProtocolBufferException;
import generate.protos.CreateOrderProtos;
import generate.protos.DaprExamplesProtos;
import generate.protos.DataToPublishProtos;
import io.dapr.DaprClientGrpc;
import io.dapr.DaprClientProtos;
import io.grpc.Server;
import io.grpc.ServerBuilder;
import io.grpc.stub.StreamObserver;
import org.apache.ibatis.io.Resources;
import org.apache.ibatis.session.SqlSession;
import org.apache.ibatis.session.SqlSessionFactory;
import org.apache.ibatis.session.SqlSessionFactoryBuilder;
import server.mybatis.Order;

import java.io.IOException;
import java.io.InputStream;
import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.List;
import java.util.TimeZone;
import java.util.UUID;

public class GrpcHelloWorldDaprService extends DaprClientGrpc.DaprClientImplBase {

    /**
     * Format to output date and time.
     */
    private static final DateFormat DATE_FORMAT = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss.SSS");

    /**
     * Server mode: Grpc server.
     */
    private Server server;

    private PublishMessageClient publishMessageClient;

    /**
     * Server mode: starts listening on given port.
     *
     * @param port Port to listen on.
     * @throws IOException Errors while trying to start service.
     */
    public void start(int port) throws IOException {

        publishMessageClient = new PublishMessageClient(port);

        this.server = ServerBuilder
                .forPort(port)
                .addService(this)
                .build()
                .start();
        System.out.printf("Server: started listening on port %d\n", port);

        // Now we handle ctrl+c (or any other JVM shutdown)
        Runtime.getRuntime().addShutdownHook(new Thread(() -> {
            System.out.println("Server: shutting down gracefully ...");
            server.shutdown();
            System.out.println("Server: Bye.");
        }));
    }

    /**
     * Server mode: waits for shutdown trigger.
     *
     * @throws InterruptedException Propagated interrupted exception.
     */
    public void awaitTermination() throws InterruptedException {
        if (this.server != null) {
            this.server.awaitTermination();
        }
    }

    /**
     * Server mode: this is the Dapr method to receive Invoke operations via Grpc.
     *
     * @param request          Dapr envelope request,
     * @param responseObserver Dapr envelope response.
     */
    @Override
    public void onInvoke(DaprClientProtos.InvokeEnvelope request, StreamObserver<Any> responseObserver) {
        try {
            switch (request.getMethod()) {
                case "say":
                    // IMPORTANT: do not use Any.unpack(), use Type.ParseFrom() instead.
                    DaprExamplesProtos.SayRequest sayRequest = DaprExamplesProtos.SayRequest.parseFrom(request.getData().getValue());
                    DaprExamplesProtos.SayResponse sayResponse = this.say(sayRequest);
                    responseObserver.onNext(Any.pack(sayResponse));
                    break;
                case "createOrder":
                    CreateOrderProtos.CreateOrderRequest createOrderRequest = CreateOrderProtos.CreateOrderRequest.parseFrom(request.getData().getValue());
                    CreateOrderProtos.CreateOrderResponse createOrderResponse = this.createOrder(createOrderRequest);

                    DataToPublishProtos.StorageReduceData storageReduceData = DataToPublishProtos.StorageReduceData.newBuilder().setProductID(createOrderRequest.getProductID()).setAmount(createOrderRequest.getAmount()).build();
                    publishMessageClient.PublishToStorageReduce(storageReduceData);
                    responseObserver.onNext(Any.pack(createOrderResponse));
                    break;
                case "getOrderList":
                    CreateOrderProtos.GetOrderListRequest getOrderListRequest = CreateOrderProtos.GetOrderListRequest.parseFrom(request.getData().getValue());
                    CreateOrderProtos.GetOrderListResponse getOrderListResponse = this.getOrderList(getOrderListRequest);
                    responseObserver.onNext(Any.pack(getOrderListResponse));
                    break;
                case "retrieveOrder":
                    CreateOrderProtos.RetrieveOrderRequest retrieveOrderRequest = CreateOrderProtos.RetrieveOrderRequest.parseFrom(request.getData().getValue());
                    CreateOrderProtos.RetrieveOrderResponse retrieveOrderResponse = this.retrieveOrder(retrieveOrderRequest);
                    responseObserver.onNext(Any.pack(retrieveOrderResponse));
                    break;
            }
        } catch (InvalidProtocolBufferException e) {
            e.printStackTrace();
            responseObserver.onError(e);
        } finally {
            responseObserver.onCompleted();
        }
    }

    /**
     * Handling of the 'say' method.
     *
     * @param request Request to say something.
     * @return Response with when it was said.
     */
    public DaprExamplesProtos.SayResponse say(DaprExamplesProtos.SayRequest request) {
        Calendar utcNow = Calendar.getInstance(TimeZone.getTimeZone("GMT"));
        String utcNowAsString = DATE_FORMAT.format(utcNow.getTime());

        // Handles the request by printing message.
        System.out.println("Server: " + request.getMessage() + " @ " + utcNowAsString);

        // Now respond with current timestamp.
        DaprExamplesProtos.SayResponse.Builder responseBuilder = DaprExamplesProtos.SayResponse.newBuilder();
        return responseBuilder.setTimestamp(utcNowAsString).build();
    }

    /**
     * Handling of the 'createOrder' method
     *
     * @param request Request to create order
     * @return Response with create result
     */
    public CreateOrderProtos.CreateOrderResponse createOrder(CreateOrderProtos.CreateOrderRequest request) {
        CreateOrderProtos.CreateOrderResponse.Builder response = CreateOrderProtos.CreateOrderResponse.newBuilder();
        try {
            SqlSession session = initSqlSession();
            Order order = convertRequestToOrder(request);
            session.insert("OrderMapper.insertOrder", order);
            session.commit();
            response.setSucceed(true);
        } catch (IOException e) {
            response.setSucceed(false);
        }
        return response.build();
    }

    /**
     * Handling of the 'getOrderList' method
     *
     * @param request Request to get order list by customer id
     * @return order list
     */
    public CreateOrderProtos.GetOrderListResponse getOrderList(CreateOrderProtos.GetOrderListRequest request) {
        CreateOrderProtos.GetOrderListResponse.Builder response = CreateOrderProtos.GetOrderListResponse.newBuilder();
        try {
            SqlSession session = initSqlSession();
            Order queryOrder = new Order();
            queryOrder.setCustomerID(request.getCustomerID());

            List<Order> orders = session.selectList("OrderMapper.selectOrders", queryOrder);
            for (Order order : orders) {
                CreateOrderProtos.Order.Builder tempOrder = CreateOrderProtos.Order.newBuilder();
                tempOrder.setAmount(order.getAmount());
                tempOrder.setCustomerID(order.getCustomerID());
                tempOrder.setID(order.getID());
                tempOrder.setProductID(order.getProductID());
                response.addOrders(tempOrder);
            }
        } catch (IOException e) {
            e.printStackTrace();
        }
        return response.build();
    }

    /**
     * Handling of the 'retrieveOrder' method
     *
     * @param request Request to retrieve order by order id
     * @return oder
     */
    public CreateOrderProtos.RetrieveOrderResponse retrieveOrder(CreateOrderProtos.RetrieveOrderRequest request) {
        CreateOrderProtos.RetrieveOrderResponse.Builder response = CreateOrderProtos.RetrieveOrderResponse.newBuilder();
        try {
            SqlSession session = initSqlSession();

            Order order = session.selectOne("OrderMapper.selectOrder", request.getOrderID());
            if (order == null) {
                return response.build();
            }
            CreateOrderProtos.Order.Builder tempOrder = CreateOrderProtos.Order.newBuilder();
            tempOrder.setAmount(order.getAmount());
            tempOrder.setCustomerID(order.getCustomerID());
            tempOrder.setID(order.getID());
            tempOrder.setProductID(order.getProductID());
            response.setOrder(tempOrder);
        } catch (IOException e) {
            e.printStackTrace();
        }
        return response.build();
    }

    /**
     * Convert Request to Order model
     *
     * @param request Convert to order model
     * @return Concert Result
     */
    private Order convertRequestToOrder(CreateOrderProtos.CreateOrderRequest request) {
        Order order = new Order();
        order.setID(UUID.randomUUID().toString());
        order.setAmount(request.getAmount());
        order.setCustomerID(request.getCustomerID());
        order.setProductID(request.getProductID());
        return order;
    }

    /**
     * Initial SqlSession
     *
     * @return sqlSession
     * @throws IOException the exception throw out
     */
    private static SqlSession initSqlSession() throws IOException {
        // mybatis-config.xml
        String resource = "mybatis-config.xml";

        InputStream is = Resources.getResourceAsStream(resource);
        SqlSessionFactory sqlSessionFactory = new SqlSessionFactoryBuilder().build(is);

        return sqlSessionFactory.openSession();
    }
}
