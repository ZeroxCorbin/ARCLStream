# ARCLStream

## This is a C# dll to facilitate asycronous communications using the ARCL protocol.

### Here is an example of using the ARCLConnection;

    using ARCL;
    using ARCLTypes;

    ARCLConnection Connection;

    private bool Connect()
    {
      string connectString = ARCLConnection.GenerateConnectionString("192.168.1.1", 7171, "adept");

      if (ARCLConnection.ValidateConnectionString(connectString))
      {
              Connection = new ARCLConnection(connectString);
      }
      else
      {
              return false;
      }

      if (Connection.Connect(true))
      {
              //Connected and logged in. Do work...

              return true;
      }
      else
      {
              return false;
      }
    }
	
### Here is an example of attaching to an event and starting the Async Thread.
#### The ARCLDataReceived event will fire on any message recieved. ARCLEventArgs contains the unprocessed string that was recieved.

      if (Connection.Connect(true))
      {
		  Connection.ARCLDataReceived += Connection_ARCLDataReceived;
		  Connection.StartRecieveAsync();

          return true;
      }
      else
      {
          return false;
      }

	  private void Connection_ARCLDataReceived(object sender, ARCLEventArgs data)
	  {
		  throw new NotImplementedException();
	  }

# THIS IS A WORK IN PROGRESS. Some of the functions, methods, and properties do nothing or do not work with the event driven core.
