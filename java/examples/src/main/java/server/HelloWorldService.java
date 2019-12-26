package server;

import org.apache.commons.cli.CommandLine;
import org.apache.commons.cli.CommandLineParser;
import org.apache.commons.cli.DefaultParser;
import org.apache.commons.cli.Options;
import server.dapr.grpc.services.GrpcHelloWorldDaprService;

/**
 * 1. Build and install jars:
 * mvn clean install
 * 2. Run in server mode:
 * dapr run --app-id hellogrpc --app-port 5000 --protocol grpc -- mvn exec:java -pl=examples -Dexec.mainClass=server.HelloWorldService -Dexec.args="-p 5000"
 */
public class HelloWorldService {

    public static void main(String[] args) throws Exception {
        Options options = new Options();
        options.addRequiredOption("p", "port", true, "Port to listen or send event to.");

        CommandLineParser parser = new DefaultParser();
        CommandLine cmd = parser.parse(options, args);

        // If port string is not valid, it will throw an exception.
        int port = Integer.parseInt(cmd.getOptionValue("port"));

        final GrpcHelloWorldDaprService service = new GrpcHelloWorldDaprService();

        service.start(port);
        service.awaitTermination();
    }
}
