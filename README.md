[![Docker Image CI](https://github.com/veloek/tevling/actions/workflows/docker-image.yml/badge.svg)](https://github.com/veloek/tevling/actions/workflows/docker-image.yml)

# Tevling

Use activities from Strava to challenge your fellow athletes in a friendly competition.

Simply sign in with your Strava account and your activities will be automatically synced.

## User handling and authentication

Tevling uses Strava as it's identity provider, requiring only email and name of the contestant.

Parts of activity information necessary to take part in challenges are stored in Tevling's database.

## Strava integration

The user will be asked to allow Tevling access to workout data from Strava. Once access is granted,
all new workout data will be pushed from Strava to Tevling through a webhook subscription.

## Installation

The app is published as a Docker container and can be run standalone or in a kubernetes cluster.

### Docker

```
docker run --volume /tmp/tevling:/app/storage -p 8080:8080 veloek/tevling \
  --env STRAVACONFIG__CLIENTID=${STRAVA_CLIENTID} \
  --env STRAVACONFIG__CLIENTSECRET=${STRAVA_CLIENTSECRET} \
  --env STRAVACONFIG__REDIRECTURI=${STRAVA_REDIRECTURI} \
  --env STRAVACONFIG__SUBSCRIPTIONID=${STRAVA_SUBSCRIPTIONID} \
  --env STRAVACONFIG__VERIFYTOKEN=${STRAVA_VERIFYTOKEN}
```

### Helm

A Helm chart is available in the `helm` directory. To install, clone the repo and run:

```
helm upgrade tevling --install --namespace tevling --create-namespace helm \
    --set-string strava.clientId=${STRAVA_CLIENTID} \
    --set-string strava.clientSecret=${STRAVA_CLIENTSECRET} \
    --set-string strava.redirectUri=${STRAVA_REDIRECTURI} \
    --set-string strava.subscriptionId=${STRAVA_SUBSCRIPTIONID} \
    --set-string strava.verifyToken=${STRAVA_VERIFYTOKEN}
```

This will configure a deployment, a secret and a pvc in addition to a service/ingress for incoming
traffic. See configuration options in `values.yaml`.

## Contribution

The app is based on ASP.NET using the Blazor framework. Simplicity is highly valued to have the app
maintainable as it's not anybody's dayjob.

Debugging should work out of the box in Visual Studio or VS Code (with C# extensions), but to simply
build (and run) the app only the .NET 8 SDK is required. Use `dotnet watch` to detect changes.

```
dotnet run --project Tevling/Tevling.csproj
```

Bug reports and PRs are very welcome!

## License

GNU GPLv3 or later.

## Author

Vegard Løkken <vegard@loekken.org>
