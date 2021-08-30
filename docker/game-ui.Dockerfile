FROM bitnami/git:latest as version
WORKDIR /src
COPY ./.git ./.git
RUN git rev-parse --short HEAD > version-suffix

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
COPY ./version.txt ./version-prefix
COPY --from=version /src/version-suffix ./version-suffix
RUN verpre="$(cat "version-prefix")" && \
    versuf="$(cat "version-suffix")" && \
    chmod +x preprocess-elm.sh && \
    ./preprocess-elm.sh && \
    cd bin && \
    rm -r src/Debug && \
    sed -i "s/version = \".*\"/version = \"$verpre-$versuf\"/" \
        src/Config.elm && \
    mkdir /content && \
    elm make --optimize --output=/content/index.js src/Main.elm

FROM node:latest as js-compressor
RUN npm install uglify-js --global
WORKDIR /content
COPY --from=builder /content/index.js /content/source.js
RUN uglifyjs \
        source.js \
        --compress 'pure_funcs=[F2,F3,F4,F5,F6,F7,F8,F9,A2,A3,A4,A5,A6,A7,A8,A9],pure_getters,unsafe_comps,unsafe' \
        --mangle 'reserved=[F2,F3,F4,F5,F6,F7,F8,F9,A2,A3,A4,A5,A6,A7,A8,A9]' \
        --output index.js && \
    rm source.js

FROM node:latest as css-compressor
RUN npm install -g csso-cli && \
    npm install -g clean-css-cli
WORKDIR /content
COPY ./content /content
RUN mkdir -p bin && \
    cd css && \
    cleancss --inline all -O2 --source-map \
        -o style.min.css \
        style.css && \
    cd .. && \
    csso -i css/style.min.css \
        --input-source-map css/style.min.css.map \
        -o bin/style.css \
        -s file

FROM bitnami/git:latest as vendor
WORKDIR /src
COPY . .
RUN git submodule update --init --recursive

FROM httpd:2.4
COPY --from=js-compressor /content /usr/local/apache2/htdocs/content
COPY --from=vendor /src/content /usr/local/apache2/htdocs/content
COPY --from=css-compressor /content/bin /usr/local/apache2/htdocs/content/css
COPY ./test-report.html /usr/local/apache2/htdocs/content/test-report.html
COPY ./docker/httpd.conf /usr/local/apache2/conf/httpd.conf
