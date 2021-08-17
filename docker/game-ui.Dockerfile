FROM ubuntu:latest as builder
RUN apt-get -qq update -y && \
    apt-get -qq upgrade -y && \
    apt-get install wget -y && \
    wget -q https://github.com/elm/compiler/releases/download/0.19.1/binary-for-linux-64-bit.gz \
        -O elm.gz && \
    gunzip elm.gz && \
    chmod +x elm && \
    mv elm /bin/
WORKDIR /src
COPY ./elm.json /src/
COPY ./src /src/src
COPY ./preprocess-elm.sh /src/
RUN chmod +x preprocess-elm.sh && \
    ./preprocess-elm.sh && \
    cd bin && \
    mkdir /content && \
    elm make --output=/content/index.js src/Main.elm

FROM bitnami/git:latest as vendor
WORKDIR /src
COPY . .
RUN git submodule update --init --recursive

FROM httpd:2.4
COPY --from=builder /content /usr/local/apache2/htdocs/content
COPY --from=vendor /src/content /usr/local/apache2/htdocs/content
COPY ./docker/httpd.conf /usr/local/apache2/conf/httpd.conf
