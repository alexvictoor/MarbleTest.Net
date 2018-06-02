FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /app

# copy csproj and restore as distinct layers
# COPY *.sln .
# COPY src/MarbleTest.Net/MarbleTest.Net.csproj ./src/MarbleTest.Net/MarbleTest.Net.csproj
# COPY test/MarbleTest.Net.Test/MarbleTest.Net.Test.csproj ./test/MarbleTest.Net.Test/MarbleTest.Net.Test.csproj
# RUN cat /app/src/MarbleTest.Net/MarbleTest.Net.csproj
# RUN cat /app/test/MarbleTest.Net.Test/MarbleTest.Net.Test.csproj
# RUN dotnet restore

# copy everything else and build app
COPY . .
WORKDIR /app
RUN cat MarbleTest.Net.sln
RUN dotnet build


FROM build AS testrunner
WORKDIR /app/test
ENTRYPOINT ["dotnet", "test", "--logger:trx"]


FROM build AS test
WORKDIR /app/test
RUN dotnet test


# FROM build AS publish
# WORKDIR /app/dotnetapp
# # add IL Linker package
# RUN dotnet add package ILLink.Tasks -v 0.1.4-preview-981901 -s https://dotnet.myget.org/F/dotnet-core/api/v3/index.json
# RUN dotnet publish -c Release -r linux-musl-x64 -o out /p:ShowLinkerSizeComparison=true


# FROM microsoft/dotnet:2.1-runtime-deps-alpine AS runtime
# WORKDIR /app
# COPY --from=publish /app/dotnetapp/out ./
# ENTRYPOINT ["./dotnetapp"]
