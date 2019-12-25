package models

import (
	"github.com/google/uuid"
	"github.com/jinzhu/gorm"
)

type Customer struct {
	ID   uuid.UUID `gorm:"primary_key;type:varchar(36)"`
	Name string
}

func (customer *Customer) BeforeCreate(scope *gorm.Scope) error {
	if idField, ok := scope.FieldByName("ID"); ok {
		if idField.IsBlank {
			idField.Set(uuid.New())
		}
	}
	return nil
}
