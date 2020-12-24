# eneter-net
Lightweight but comprehensive framework for interprocess communication.

Here is a summary of features provided by the framework:

## Supported Communication Protocols and Transportation Mechanisms
To connect applications, the framework supports following protocols and transportation mechanisms:
* _TCP_ - for communication between applications running on different machines.
* _Websockets_ - for communication between applications running on different machines. It provides bi-directional, full-duplex communication in environments which block non-standard Internet connections using a firewall.
* _HTTP_ - for communication between applications running on different machines. It works in environments blocking non-standard Internet connections using a firewall.
* _UDP_ - for communication between applications running on different machines. Short and fast messages. Delivery of packets is not guaranteed.
* _Shared Memory_ - for very fast communication between applications running on the same machine.
* _Named Pipes_ - for communication between applications running on the same machine.
* _Android USB Cable_ - for communication between computer and Android device connected via the USB cable.
* _Thread Messaging_ - for communication between threads running in the same process.

## Serialization of Messages
Communicated messages can be serialized/deserialized with:
* _XML Serializer_ - serialization to XML.
* _Xml Data Contract Serializer_ - for serialization to XML using DataContract and DataMember attributes.
* _Json Data Contract Serializer_ - for serialization to JSON using DataContract and DataMember attributes.
* _Binary Serializer_ - for fast serialization using the binary format (does not work for the communication between .NET and Java).
* _AES Serializer_ - for encrypting using Advanced Encryption Standard.
* _Rijndael Serializer_ - for encrypting using Rijndael encryption.
* _GZip Serializer_ - for compressing big messages before sending across the network.
* _RSA Serializer_ - for encrypting messages by RSA algorithm using public and private keys.
* _RSA Digitally Signing Serializer_ - for using digital signature to verify who sent the message and that the message was not changed.

**The communication API is not bound to a particular protocol or encoding format, therefore your implementation is same, does not matter what you use.**

## Sending-Receiving Messages
To implement the communication between applications, the framework provides functionality to send and receive messages. Communication can be one-way or request-response and messages can be:
* _String Messages_ - for sending and receiving text messages.
* _Typed Messages_ - for sending and receiving data structures of specified types.
* _RPC (Remote Procedure Calls)_ - for invoking methods or subscribing events in another application.

**RPC works across platforms! It means you can make the RPC communication between C# and Java code.**

## Routing Messages
Components that can be placed on the communication path to control the routing behavior.
* _Message Bus_ - for publishing multiple services from one place.
* _Broker_ - for publish-subscribe scenarios (publisher sends messages to the broker and the broker notifies all subscribers).
* _Router_ - for re-routing messages to a different address.
* _Backup Router_ - for re-routing messages to a backup service if the service is not available.
* _Dispatcher_ - for routing messages to all connected receivers (e.g. for a service listening to TCP and HTTP at the same time).
* _Load Balancer_ - for distributing workload across multiple computers.
* _Channel Wrapper/Unwrapper_ - for receiving multiple types of messages on one address (no need for if ... else ... statement in the user code)

## Reliability
The communication across the network is typically less reliable as a local call inside a process. The network connection can be interrupted or a receiving application can be temporarily unavailable. If your communication scenario requires overcome these issues, the framework provides:
* _Monitoring of connection availability_ - for early detection of a disconnection.
* _Buffered messaging and automatic reconnect_ - for overcoming short disconnections (e.g. in case of unstable network).

## Security
The communication across the network is easy to observe and confidential data can be acquired by unauthorized persons. Therefore, you may want to protect your data:
* _Authenticated Connection_ - for verifying the identity of clients connecting to the service.
* _Encrypted Messages_ - AES or RSA serializers.
* _Digitaly Signed Messages_ - for protecting authenticity and message integrity.
* _SSL/TLS_ - for secured communication.

