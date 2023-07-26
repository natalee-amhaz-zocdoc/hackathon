FROM 038405485655.dkr.ecr.us-east-1.amazonaws.com/dotnet-base:6.0
WORKDIR /app
EXPOSE 443
COPY /publish/Web/. .
ENTRYPOINT [ "/app/entrypoint.sh" ]
