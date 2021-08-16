FROM mongo
RUN echo "rs.initiate();" > /docker-entrypoint-initdb.d/rs-init.js && \
    echo "#!/bin/bash" > /start.sh && \
    echo "echo \"\$@\"" >> /start.sh && \
    echo "cp /keyfile /keyfile2" >> /start.sh && \
    echo "chown \$(id -u):\$(id -g) /keyfile2" >> /start.sh && \
    echo "chmod 0400 /keyfile2" >> /start.sh && \
    echo "/usr/local/bin/docker-entrypoint.sh \"\$@\"" >> /start.sh && \
    chmod +x /start.sh
ENTRYPOINT [ "/start.sh" ]
# CMD [ "mongod", "--replSet", "werewolf", "--keyFile", "/keyfile2" ]
CMD [ "mongod", "--replSet", "werewolf" ]
