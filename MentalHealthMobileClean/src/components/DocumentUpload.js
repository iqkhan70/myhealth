import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  Alert,
  ScrollView,
  TextInput,
  Modal,
  Image,
  ActivityIndicator,
  FlatList,
  RefreshControl,
} from 'react-native';
import * as DocumentPicker from 'expo-document-picker';
import * as ImagePicker from 'expo-image-picker';
import * as FileSystem from 'expo-file-system';
import DocumentUploadService from '../services/DocumentUploadService';

// Map file type to ContentTypeEnum (matches server enum)
// ContentTypeEnum: Document = 1, Image = 2, Video = 3, Audio = 4, Other = 5
const mapFileTypeToContentTypeEnum = (fileType, mimeType) => {
  if (fileType === 'image' || mimeType?.startsWith('image/')) {
    return 2; // Image
  } else if (fileType === 'video' || mimeType?.startsWith('video/')) {
    return 3; // Video
  } else if (fileType === 'audio' || mimeType?.startsWith('audio/')) {
    return 4; // Audio
  } else if (fileType === 'Document' || mimeType?.includes('pdf') || mimeType?.includes('word') || mimeType?.includes('document')) {
    return 1; // Document
  } else {
    return 1; // Default to Document for PDFs, Word docs, etc.
  }
};

const DocumentUpload = ({ 
  patientId, 
  serviceRequestId = null, // New: Service Request ID to tie content to
  onDocumentUploaded,
  showPatientSelector = false,
  availablePatients = [],
  onPatientSelect,
  user = null // Current user to check role for delete permission
}) => {
  const [isUploading, setIsUploading] = useState(false);
  const [documents, setDocuments] = useState([]);
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [showUploadModal, setShowUploadModal] = useState(false);
  const [selectedFile, setSelectedFile] = useState(null);
  const [uploadForm, setUploadForm] = useState({
    title: '',
    description: '',
    category: '',
  });
  const [categories, setCategories] = useState([]);
  const [selectedPatient, setSelectedPatient] = useState(patientId);

  useEffect(() => {
    if (patientId) {
      loadCategories();
      setSelectedPatient(patientId); // Sync selectedPatient with patientId prop
    }
  }, [patientId]);

  // Load documents when selectedPatient or serviceRequestId changes
  useEffect(() => {
    if (selectedPatient) {
      loadDocuments();
    }
  }, [selectedPatient, serviceRequestId, loadDocuments]);

  const loadCategories = async () => {
    try {
      const cats = await DocumentUploadService.getCategories();
      setCategories(cats);
    } catch (error) {
      console.error('Error loading categories:', error);
    }
  };

  const loadDocuments = useCallback(async () => {
    if (!selectedPatient) return;
    
    setLoading(true);
    try {
      const response = await DocumentUploadService.getDocuments(selectedPatient, {}, serviceRequestId);
      setDocuments(response.documents || []);
    } catch (error) {
      console.error('Error loading documents:', error);
      Alert.alert('Error', 'Failed to load documents');
    } finally {
      setLoading(false);
    }
  }, [selectedPatient, serviceRequestId]);

  const onRefresh = async () => {
    setRefreshing(true);
    await loadDocuments();
    setRefreshing(false);
  };

  const selectFile = async () => {
    try {
      // Show action sheet for file selection
      Alert.alert(
        'Select File Type',
        'Choose the type of file you want to upload',
        [
          {
            text: 'Camera',
            onPress: () => selectFromCamera(),
          },
          {
            text: 'Photo Library',
            onPress: () => selectFromLibrary(),
          },
          {
            text: 'Documents',
            onPress: () => selectDocument(),
          },
          {
            text: 'Cancel',
            style: 'cancel',
          },
        ]
      );
    } catch (error) {
      console.error('Error selecting file:', error);
      Alert.alert('Error', 'Failed to select file');
    }
  };

  const selectFromCamera = async () => {
    try {
      const { status } = await ImagePicker.requestCameraPermissionsAsync();
      if (status !== 'granted') {
        Alert.alert('Permission Required', 'Camera permission is required to take photos');
        return;
      }

      const result = await ImagePicker.launchCameraAsync({
        mediaTypes: ImagePicker.MediaTypeOptions.All,
        allowsEditing: true,
        aspect: [4, 3],
        quality: 0.8,
      });

      if (!result.canceled && result.assets[0]) {
        const asset = result.assets[0];
        const fileInfo = DocumentUploadService.getFileInfo(asset.uri);
        
        setSelectedFile({
          uri: asset.uri,
          name: fileInfo.fileName,
          type: fileInfo.type,
          contentType: fileInfo.contentType,
          size: asset.fileSize || 0,
        });
        
        setUploadForm(prev => ({
          ...prev,
          title: fileInfo.fileName.split('.')[0],
        }));
        
        setShowUploadModal(true);
      }
    } catch (error) {
      console.error('Error taking photo:', error);
      Alert.alert('Error', 'Failed to take photo');
    }
  };

  const selectFromLibrary = async () => {
    try {
      const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
      if (status !== 'granted') {
        Alert.alert('Permission Required', 'Photo library permission is required');
        return;
      }

      const result = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ImagePicker.MediaTypeOptions.All,
        allowsEditing: true,
        aspect: [4, 3],
        quality: 0.8,
      });

      if (!result.canceled && result.assets[0]) {
        const asset = result.assets[0];
        const fileInfo = DocumentUploadService.getFileInfo(asset.uri);
        
        setSelectedFile({
          uri: asset.uri,
          name: fileInfo.fileName,
          type: fileInfo.type,
          contentType: fileInfo.contentType,
          size: asset.fileSize || 0,
        });
        
        setUploadForm(prev => ({
          ...prev,
          title: fileInfo.fileName.split('.')[0],
        }));
        
        setShowUploadModal(true);
      }
    } catch (error) {
      console.error('Error selecting from library:', error);
      Alert.alert('Error', 'Failed to select from library');
    }
  };

  const selectDocument = async () => {
    try {
      const result = await DocumentPicker.getDocumentAsync({
        type: '*/*',
        copyToCacheDirectory: true,
      });

      if (!result.canceled && result.assets[0]) {
        const asset = result.assets[0];
        const fileInfo = DocumentUploadService.getFileInfo(asset.uri);
        
        setSelectedFile({
          uri: asset.uri,
          name: asset.name,
          type: fileInfo.type,
          contentType: asset.mimeType || fileInfo.contentType,
          size: asset.size || 0,
        });
        
        setUploadForm(prev => ({
          ...prev,
          title: asset.name.split('.')[0],
        }));
        
        setShowUploadModal(true);
      }
    } catch (error) {
      console.error('Error selecting document:', error);
      Alert.alert('Error', 'Failed to select document');
    }
  };

  const uploadDocument = async () => {
    if (!selectedFile || !selectedPatient) {
      Alert.alert('Error', 'Please select a file and patient');
      return;
    }

    if (!uploadForm.title.trim()) {
      Alert.alert('Error', 'Please enter a title for the document');
      return;
    }

    setIsUploading(true);
    try {
      // Map to server's expected property names (PascalCase)
      // Also need to map file type to ContentTypeEnum
      const contentTypeEnum = mapFileTypeToContentTypeEnum(selectedFile.type, selectedFile.contentType);
      
      // Ensure ServiceRequestId is set - if null, this is a problem
      if (!serviceRequestId) {
        Alert.alert('Error', 'ServiceRequestId is required. Please select a Service Request first.');
        setIsUploading(false);
        return;
      }

      const uploadRequest = {
        PatientId: selectedPatient, // PascalCase
        ServiceRequestId: serviceRequestId, // PascalCase - tie content to ServiceRequest
        Title: uploadForm.title.trim(), // PascalCase
        Description: uploadForm.description.trim() || null, // Optional
        FileName: selectedFile.name, // PascalCase
        MimeType: selectedFile.contentType, // PascalCase (was contentType)
        FileSizeBytes: selectedFile.size, // PascalCase
        Type: contentTypeEnum, // PascalCase, must be ContentTypeEnum value
        Category: uploadForm.category || null, // Optional
      };

      await DocumentUploadService.uploadDocument(selectedFile.uri, uploadRequest);
      
      Alert.alert('Success', 'Document uploaded successfully!');
      
      // Reset form
      setSelectedFile(null);
      setUploadForm({ title: '', description: '', category: '' });
      setShowUploadModal(false);
      
      // Reload documents
      await loadDocuments();
      
      // Notify parent
      if (onDocumentUploaded) {
        onDocumentUploaded();
      }
    } catch (error) {
      console.error('Error uploading document:', error);
      Alert.alert('Error', error.message || 'Failed to upload document');
    } finally {
      setIsUploading(false);
    }
  };

  const downloadDocument = async (document) => {
    try {
      const downloadUrl = await DocumentUploadService.getDownloadUrl(document.id);
      if (downloadUrl) {
        // For React Native, you might want to use Linking or a file download library
        Alert.alert('Download', `Download URL: ${downloadUrl}`);
      }
    } catch (error) {
      console.error('Error downloading document:', error);
      Alert.alert('Error', 'Failed to download document');
    }
  };

  const deleteDocument = async (document) => {
    Alert.alert(
      'Delete Document',
      'Are you sure you want to delete this document?',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Delete',
          style: 'destructive',
          onPress: async () => {
            try {
              await DocumentUploadService.deleteDocument(document.id);
              Alert.alert('Success', 'Document deleted successfully');
              await loadDocuments();
            } catch (error) {
              console.error('Error deleting document:', error);
              Alert.alert('Error', 'Failed to delete document');
            }
          },
        },
      ]
    );
  };

  const renderDocument = ({ item }) => {
    // Only admins (roleId === 3) can delete documents
    const canDelete = user?.roleId === 3;
    
    return (
      <View style={styles.documentCard}>
        <View style={styles.documentHeader}>
          <Text style={styles.documentTitle}>{item.title}</Text>
          <View style={styles.documentActions}>
            <TouchableOpacity
              style={styles.actionButton}
              onPress={() => downloadDocument(item)}
            >
              <Text style={styles.actionButtonText}>Download</Text>
            </TouchableOpacity>
            {canDelete && (
              <TouchableOpacity
                style={[styles.actionButton, styles.deleteButton]}
                onPress={() => deleteDocument(item)}
              >
                <Text style={[styles.actionButtonText, styles.deleteButtonText]}>Delete</Text>
              </TouchableOpacity>
            )}
          </View>
        </View>
      
      {item.description ? (
        <Text style={styles.documentDescription}>{item.description}</Text>
      ) : null}
      
        <View style={styles.documentMeta}>
          <Text style={styles.metaText}>File: {item.originalFileName}</Text>
          <Text style={styles.metaText}>Size: {DocumentUploadService.formatFileSize(item.fileSizeBytes)}</Text>
          <Text style={styles.metaText}>Added: {new Date(item.createdAt).toLocaleDateString()}</Text>
          <Text style={styles.metaText}>By: {item.addedByUserName}</Text>
        </View>
      </View>
    );
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>Documents</Text>
        <TouchableOpacity style={styles.uploadButton} onPress={selectFile}>
          <Text style={styles.uploadButtonText}>+ Upload</Text>
        </TouchableOpacity>
      </View>

      {showPatientSelector && (
        <View style={styles.patientSelector}>
          <Text style={styles.selectorLabel}>Select Client:</Text>
          <ScrollView horizontal showsHorizontalScrollIndicator={false}>
            {availablePatients.map((patient) => (
              <TouchableOpacity
                key={patient.id}
                style={[
                  styles.patientButton,
                  selectedPatient === patient.id && styles.selectedPatientButton,
                ]}
                onPress={() => {
                  setSelectedPatient(patient.id);
                  if (onPatientSelect) {
                    onPatientSelect(patient);
                  }
                  // Documents will be loaded automatically via useEffect when selectedPatient changes
                }}
              >
                <Text
                  style={[
                    styles.patientButtonText,
                    selectedPatient === patient.id && styles.selectedPatientButtonText,
                  ]}
                >
                  {patient.firstName} {patient.lastName}
                </Text>
              </TouchableOpacity>
            ))}
          </ScrollView>
        </View>
      )}

      {loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color="#007bff" />
          <Text style={styles.loadingText}>Loading documents...</Text>
        </View>
      ) : (
        <FlatList
          data={documents}
          renderItem={renderDocument}
          keyExtractor={(item) => item.id.toString()}
          refreshControl={
            <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
          }
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <Text style={styles.emptyText}>No documents found</Text>
            </View>
          }
        />
      )}

      {/* Upload Modal */}
      <Modal
        visible={showUploadModal}
        animationType="slide"
        presentationStyle="pageSheet"
      >
        <View style={styles.modalContainer}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>Upload Document</Text>
            <TouchableOpacity
              style={styles.closeButton}
              onPress={() => setShowUploadModal(false)}
            >
              <Text style={styles.closeButtonText}>✕</Text>
            </TouchableOpacity>
          </View>

          <ScrollView style={styles.modalContent}>
            {selectedFile && (
              <View style={styles.filePreview}>
                <Text style={styles.fileName}>{selectedFile.name}</Text>
                <Text style={styles.fileSize}>
                  {DocumentUploadService.formatFileSize(selectedFile.size)}
                </Text>
              </View>
            )}

            <TextInput
              style={styles.input}
              placeholder="Document Title"
              value={uploadForm.title}
              onChangeText={(text) => setUploadForm(prev => ({ ...prev, title: text }))}
            />

            <TextInput
              style={[styles.input, styles.textArea]}
              placeholder="Description (optional)"
              value={uploadForm.description}
              onChangeText={(text) => setUploadForm(prev => ({ ...prev, description: text }))}
              multiline
              numberOfLines={3}
            />

            <View style={styles.categoryContainer}>
              <Text style={styles.categoryLabel}>Category:</Text>
              <ScrollView horizontal showsHorizontalScrollIndicator={false}>
                {categories.map((category) => (
                  <TouchableOpacity
                    key={category}
                    style={[
                      styles.categoryButton,
                      uploadForm.category === category && styles.selectedCategoryButton,
                    ]}
                    onPress={() => setUploadForm(prev => ({ ...prev, category }))}
                  >
                    <Text
                      style={[
                        styles.categoryButtonText,
                        uploadForm.category === category && styles.selectedCategoryButtonText,
                      ]}
                    >
                      {category}
                    </Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
            </View>

            <TouchableOpacity
              style={[styles.uploadConfirmButton, isUploading && styles.uploadConfirmButtonDisabled]}
              onPress={uploadDocument}
              disabled={isUploading}
            >
              {isUploading ? (
                <ActivityIndicator color="white" />
              ) : (
                <Text style={styles.uploadConfirmButtonText}>Upload Document</Text>
              )}
            </TouchableOpacity>
          </ScrollView>
        </View>
      </Modal>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8f9fa',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    backgroundColor: 'white',
    borderBottomWidth: 1,
    borderBottomColor: '#e9ecef',
  },
  headerTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#2c3e50',
  },
  uploadButton: {
    backgroundColor: '#007bff',
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 6,
  },
  uploadButtonText: {
    color: 'white',
    fontWeight: '600',
  },
  patientSelector: {
    backgroundColor: 'white',
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#e9ecef',
  },
  selectorLabel: {
    fontSize: 16,
    fontWeight: '600',
    marginBottom: 8,
    color: '#2c3e50',
  },
  patientButton: {
    backgroundColor: '#f8f9fa',
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 6,
    marginRight: 8,
    borderWidth: 1,
    borderColor: '#dee2e6',
  },
  selectedPatientButton: {
    backgroundColor: '#007bff',
    borderColor: '#007bff',
  },
  patientButtonText: {
    color: '#6c757d',
    fontWeight: '500',
  },
  selectedPatientButtonText: {
    color: 'white',
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    marginTop: 16,
    fontSize: 16,
    color: '#6c757d',
  },
  documentCard: {
    backgroundColor: 'white',
    margin: 8,
    padding: 16,
    borderRadius: 8,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  documentHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 8,
  },
  documentTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#2c3e50',
    flex: 1,
    marginRight: 8,
  },
  documentActions: {
    flexDirection: 'row',
    gap: 8,
  },
  actionButton: {
    backgroundColor: '#f8f9fa',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 4,
    borderWidth: 1,
    borderColor: '#dee2e6',
  },
  deleteButton: {
    backgroundColor: '#dc3545',
    borderColor: '#dc3545',
  },
  actionButtonText: {
    fontSize: 12,
    color: '#6c757d',
  },
  deleteButtonText: {
    color: 'white',
  },
  documentDescription: {
    fontSize: 14,
    color: '#6c757d',
    marginBottom: 8,
  },
  documentMeta: {
    gap: 4,
  },
  metaText: {
    fontSize: 12,
    color: '#adb5bd',
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 32,
  },
  emptyText: {
    fontSize: 16,
    color: '#6c757d',
  },
  modalContainer: {
    flex: 1,
    backgroundColor: 'white',
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#e9ecef',
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#2c3e50',
  },
  closeButton: {
    padding: 8,
  },
  closeButtonText: {
    fontSize: 18,
    color: '#6c757d',
  },
  modalContent: {
    flex: 1,
    padding: 16,
  },
  filePreview: {
    backgroundColor: '#f8f9fa',
    padding: 12,
    borderRadius: 6,
    marginBottom: 16,
  },
  fileName: {
    fontSize: 14,
    fontWeight: '600',
    color: '#2c3e50',
  },
  fileSize: {
    fontSize: 12,
    color: '#6c757d',
    marginTop: 4,
  },
  input: {
    borderWidth: 1,
    borderColor: '#dee2e6',
    borderRadius: 6,
    padding: 12,
    fontSize: 16,
    marginBottom: 16,
  },
  textArea: {
    height: 80,
    textAlignVertical: 'top',
  },
  categoryContainer: {
    marginBottom: 16,
  },
  categoryLabel: {
    fontSize: 16,
    fontWeight: '600',
    marginBottom: 8,
    color: '#2c3e50',
  },
  categoryButton: {
    backgroundColor: '#f8f9fa',
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 6,
    marginRight: 8,
    borderWidth: 1,
    borderColor: '#dee2e6',
  },
  selectedCategoryButton: {
    backgroundColor: '#007bff',
    borderColor: '#007bff',
  },
  categoryButtonText: {
    fontSize: 14,
    color: '#6c757d',
  },
  selectedCategoryButtonText: {
    color: 'white',
  },
  uploadConfirmButton: {
    backgroundColor: '#007bff',
    paddingVertical: 12,
    borderRadius: 6,
    alignItems: 'center',
    marginTop: 16,
  },
  uploadConfirmButtonDisabled: {
    backgroundColor: '#6c757d',
  },
  uploadConfirmButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
});

export default DocumentUpload;
