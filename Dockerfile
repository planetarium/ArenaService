FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build-env
ARG TARGETPLATFORM
ARG BUILDPLATFORM
WORKDIR /app

# Copy everything else and build
COPY . ./
RUN <<EOF
#!/bin/bash
echo "TARGETPLATFROM=$TARGETPLATFORM"
echo "BUILDPLATFORM=$BUILDPLATFORM"
if [[ "$TARGETPLATFORM" = "linux/amd64" ]]
then
  dotnet publish ArenaService/ArenaService.csproj \
    -c Release \
    -r linux-x64 \
    -o out \
    --self-contained
elif [[ "$TARGETPLATFORM" = "linux/arm64" ]]
then
  dotnet publish ArenaService/ArenaService.csproj \
    -c Release \
    -r linux-arm64 \
    -o out \
    --self-contained
else
  echo "Not supported target platform: '$TARGETPLATFORM'."
  exit -1
fi
EOF

# Build runtime image
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim
WORKDIR /app
RUN apt-get update && apt-get install -y libc6-dev
COPY --from=build-env /app/out .

# Install native deps & utilities for production
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libc6-dev jq curl \
     && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "ArenaService.dll"]
