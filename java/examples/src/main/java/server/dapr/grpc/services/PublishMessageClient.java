package server.dapr.grpc.services;

import com.google.protobuf.Any;
import generate.protos.DataToPublishProtos;
import io.dapr.DaprGrpc;
import io.dapr.DaprProtos;
import io.grpc.ManagedChannel;
import io.grpc.ManagedChannelBuilder;

public class PublishMessageClient {

    /**
     * Client communication channel: host, port and tls(on/off)
     */
    private final ManagedChannel channel;

    /**
     * Calls will be done asynchronously.
     */
    private final DaprGrpc.DaprFutureStub client;

    /**
     * Creates a Grpc client for the DaprGrpc service.
     *
     * @param port port for the remote service endpoint
     */
    public PublishMessageClient(int port) {
        this(ManagedChannelBuilder
                .forAddress("localhost", port)
                .usePlaintext()  // SSL/TLS is default, we turn it off just because this is a sample and not prod.
                .build());
    }

    /**
     * Helper constructor to build client from channel.
     *
     * @param channel
     */
    private PublishMessageClient(ManagedChannel channel) {
        this.channel = channel;
        this.client = DaprGrpc.newFutureStub(channel);
    }

    /**
     * Publish Event To StorageReduce topic
     * @param storageReduceData
     */
    public void PublishToStorageReduce(DataToPublishProtos.StorageReduceData storageReduceData) {
        DaprProtos.PublishEventEnvelope publishEventEnvelope = DaprProtos.PublishEventEnvelope.newBuilder().setTopic("Storage.Reduce").setData(Any.pack(storageReduceData)).build();
        client.publishEvent(publishEventEnvelope);
    }
}
