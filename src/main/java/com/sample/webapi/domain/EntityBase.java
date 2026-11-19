package com.sample.webapi.domain;

import jakarta.persistence.Column;
import jakarta.persistence.Id;
import jakarta.persistence.MappedSuperclass;
import java.util.UUID;

/** Base class for entities identified by a client-assigned UUID primary key. */
@MappedSuperclass
public abstract class EntityBase {

    @Id
    @Column(name = "Id", nullable = false)
    private UUID id;

    public UUID getId() {
        return id;
    }

    public void setId(UUID id) {
        this.id = id;
    }
}
