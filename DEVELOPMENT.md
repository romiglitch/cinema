# Development Setup

## Running the Cinema App in Visual Studio

The Cinema solution consists of multiple projects:
- **Movie (Shipping)** - Main web application (port 50594)
- **TrailersWS** - Web service for movie trailers (port 51730)
- **Payment** - Payment processing library
- **DALLlilbrary** - Data access layer

### Configure Multiple Startup Projects

To run both the main app and TrailersWS automatically:

1. Right-click on the **solution** in Solution Explorer
2. Select **Properties** (or **Set Startup Projects**)
3. Choose **Multiple startup projects**
4. Set the following projects to **Start**:
   - Movie
   - TrailersWS
5. Click **OK**

Now when you press F5 or click Start, both the main app and the trailer service will launch automatically.

### Alternative: Command Line

You can also run both projects from the command line:
```powershell
# From the repository root
start iisexpress /path:"Shipping" /port:50594
start iisexpress /path:"TrailersWS" /port:51730
```

### Ports
- Main app: http://localhost:50594/
- TrailersWS: http://localhost:51730/Trailers.asmx
